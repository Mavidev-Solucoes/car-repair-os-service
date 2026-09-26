using System.Text;
using System.Text.Json;
using Application.Common.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Messaging;

public class RabbitMqCommandConsumer : ICommandConsumer
{
    private const string RetryCountHeader = "x-retry-count";
    private const string RetrySuffix = ".retry";
    private const string DeadLetterSuffix = ".dlq";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;

    public RabbitMqCommandConsumer(IRabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public Task<IAsyncDisposable> StartConsumingAsync<TCommand>(
        string queueName,
        string routingKey,
        Func<MessageEnvelope<TCommand>, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var processingToken = linkedCancellationTokenSource.Token;
        var channel = _connectionProvider.CreateChannel();
        var channelLock = new object();
        DeclareTopology(channel, queueName, routingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();

            try
            {
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<MessageEnvelope<TCommand>>(json, SerializerOptions)
                    ?? throw new InvalidOperationException("Invalid RabbitMQ message payload.");

                await handler(message, processingToken);
                lock (channelLock)
                {
                    channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                }
            }
            catch (OperationCanceledException) when (processingToken.IsCancellationRequested)
            {
                lock (channelLock)
                {
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                }
            }
            catch
            {
                var retryCount = GetRetryCount(eventArgs.BasicProperties.Headers);
                var republished = retryCount < _options.MaxRetries
                    ? TryPublishToRetryExchange(channel, channelLock, body, eventArgs.BasicProperties, routingKey, retryCount + 1)
                    : TryPublishToDeadLetterExchange(channel, channelLock, body, eventArgs.BasicProperties, routingKey);

                lock (channelLock)
                {
                    if (republished)
                    {
                        channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            }
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        IAsyncDisposable subscription = new RabbitMqConsumerSubscription(channel, channelLock, consumerTag, linkedCancellationTokenSource);
        return Task.FromResult(subscription);
    }

    private void DeclareTopology(IModel channel, string queueName, string routingKey)
    {
        var retryExchangeName = GetRetryExchangeName();
        var deadLetterExchangeName = GetDeadLetterExchangeName();
        var retryQueueName = GetRetryQueueName(queueName);
        var deadLetterQueueName = GetDeadLetterQueueName(queueName);

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(retryExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(deadLetterExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queue: queueName, exchange: _options.ExchangeName, routingKey: routingKey);

        channel.QueueDeclare(
            queue: retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _options.RetryDelayMilliseconds,
                ["x-dead-letter-exchange"] = _options.ExchangeName,
                ["x-dead-letter-routing-key"] = routingKey
            });
        channel.QueueBind(queue: retryQueueName, exchange: retryExchangeName, routingKey: routingKey);

        channel.QueueDeclare(queue: deadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queue: deadLetterQueueName, exchange: deadLetterExchangeName, routingKey: routingKey);
    }

    private bool TryPublishToRetryExchange(
        IModel channel,
        object channelLock,
        byte[] body,
        IBasicProperties properties,
        string routingKey,
        int retryCount)
    {
        try
        {
            lock (channelLock)
            {
                var retryProperties = channel.CreateBasicProperties();
                retryProperties.ContentType = properties.ContentType;
                retryProperties.DeliveryMode = 2;
                retryProperties.MessageId = properties.MessageId;
                retryProperties.CorrelationId = properties.CorrelationId;
                retryProperties.Type = properties.Type;
                retryProperties.Timestamp = properties.Timestamp;
                retryProperties.Headers = CloneHeaders(properties.Headers);
                retryProperties.Headers[RetryCountHeader] = retryCount;

                channel.BasicPublish(
                    exchange: GetRetryExchangeName(),
                    routingKey: routingKey,
                    basicProperties: retryProperties,
                    body: body);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryPublishToDeadLetterExchange(
        IModel channel,
        object channelLock,
        byte[] body,
        IBasicProperties properties,
        string routingKey)
    {
        try
        {
            lock (channelLock)
            {
                var deadLetterProperties = channel.CreateBasicProperties();
                deadLetterProperties.ContentType = properties.ContentType;
                deadLetterProperties.DeliveryMode = 2;
                deadLetterProperties.MessageId = properties.MessageId;
                deadLetterProperties.CorrelationId = properties.CorrelationId;
                deadLetterProperties.Type = properties.Type;
                deadLetterProperties.Timestamp = properties.Timestamp;
                deadLetterProperties.Headers = CloneHeaders(properties.Headers);

                channel.BasicPublish(
                    exchange: GetDeadLetterExchangeName(),
                    routingKey: routingKey,
                    basicProperties: deadLetterProperties,
                    body: body);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, object> CloneHeaders(IDictionary<string, object>? headers)
    {
        return headers is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(headers);
    }

    private static int GetRetryCount(IDictionary<string, object>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryCountHeader, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var count) => count,
            int count => count,
            long count => (int)count,
            _ => 0
        };
    }

    private string GetRetryExchangeName() => $"{_options.ExchangeName}.retry";

    private string GetDeadLetterExchangeName() => $"{_options.ExchangeName}.dlq";

    private static string GetRetryQueueName(string queueName) => $"{queueName}{RetrySuffix}";

    private static string GetDeadLetterQueueName(string queueName) => $"{queueName}{DeadLetterSuffix}";

    private sealed class RabbitMqConsumerSubscription : IAsyncDisposable
    {
        private readonly IModel _channel;
        private readonly object _channelLock;
        private readonly string _consumerTag;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public RabbitMqConsumerSubscription(
            IModel channel,
            object channelLock,
            string consumerTag,
            CancellationTokenSource cancellationTokenSource)
        {
            _channel = channel;
            _channelLock = channelLock;
            _consumerTag = consumerTag;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            _cancellationTokenSource.Cancel();

            lock (_channelLock)
            {
                if (_channel.IsOpen)
                {
                    _channel.BasicCancel(_consumerTag);
                }
            }

            _channel.Dispose();
            _cancellationTokenSource.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

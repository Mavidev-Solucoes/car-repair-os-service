using System.Text;
using System.Text.Json;
using Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Threading;

namespace Infrastructure.Messaging;

public class RabbitMqCommandConsumer : ICommandConsumer
{
    private const string RetryCountHeader = "x-retry-count";
    private const string RetrySuffix = ".retry";
    private const string DeadLetterSuffix = ".dlq";
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqCommandConsumer> _logger;

    public RabbitMqCommandConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqCommandConsumer>? logger = null)
    {
        _logger = logger ?? NullLogger<RabbitMqCommandConsumer>.Instance;
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public Task<IAsyncDisposable> StartConsumingAsync(
        string queueName,
        string commandType,
        Func<IntegrationMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var routingKey = commandType;
        var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var processingToken = linkedCancellationTokenSource.Token;
        var channel = _connectionProvider.CreateChannel();
        var channelLock = new object();
        var inFlightHandlers = 0;
        DeclareTopology(channel, queueName, routingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            Interlocked.Increment(ref inFlightHandlers);
            var body = eventArgs.Body.ToArray();

            try
            {
                var message = Deserialize(body);
                await MessageTracing.ConsumeAsync(
                    message.Type,
                    message.CorrelationId,
                    eventArgs.BasicProperties?.Headers,
                    () => handler(message, processingToken));
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
            catch (Exception exception)
            {
                var retryCount = GetRetryCount(eventArgs.BasicProperties.Headers);
                var willRetry = exception is not InvalidMessageException && retryCount < _options.MaxRetries;
                _logger.Log(
                    willRetry ? LogLevel.Warning : LogLevel.Error,
                    exception,
                    willRetry
                        ? "Command {RoutingKey} failed; retry {Attempt}/{MaxRetries}."
                        : "Command {RoutingKey} failed; sending to dead-letter queue after {Attempt} attempts.",
                    routingKey,
                    retryCount + 1,
                    _options.MaxRetries);
                var republished = willRetry
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
            finally
            {
                Interlocked.Decrement(ref inFlightHandlers);
            }
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        IAsyncDisposable subscription = new RabbitMqConsumerSubscription(
            channel,
            channelLock,
            consumerTag,
            linkedCancellationTokenSource,
            () => Interlocked.CompareExchange(ref inFlightHandlers, 0, 0));
        return Task.FromResult(subscription);
    }

    private void DeclareTopology(IModel channel, string queueName, string routingKey)
    {
        var retryExchangeName = GetRetryExchangeName();
        var deadLetterExchangeName = GetDeadLetterExchangeName();
        var retryQueueName = GetRetryQueueName(queueName);
        var deadLetterQueueName = GetDeadLetterQueueName(queueName);

        channel.ExchangeDeclare(_options.CommandsExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(retryExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(deadLetterExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = deadLetterExchangeName,
                ["x-dead-letter-routing-key"] = routingKey
            });
        channel.QueueBind(queue: queueName, exchange: _options.CommandsExchangeName, routingKey: routingKey);

        channel.QueueDeclare(
            queue: retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _options.RetryDelayMilliseconds,
                ["x-dead-letter-exchange"] = _options.CommandsExchangeName,
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

    public static IntegrationMessage Deserialize(byte[] body)
    {
        IntegrationMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<IntegrationMessage>(body, IntegrationMessage.SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidMessageException($"Message body is not a valid envelope: {exception.Message}");
        }

        if (message is null || string.IsNullOrWhiteSpace(message.Type) || string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            throw new InvalidMessageException("Message envelope must have type and correlationId.");
        }

        return message;
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

    private string GetRetryExchangeName() => $"{_options.CommandsExchangeName}.retry";

    private string GetDeadLetterExchangeName() => $"{_options.CommandsExchangeName}.dlx";

    private static string GetRetryQueueName(string queueName) => $"{queueName}{RetrySuffix}";

    private static string GetDeadLetterQueueName(string queueName) => $"{queueName}{DeadLetterSuffix}";

    private sealed class RabbitMqConsumerSubscription : IAsyncDisposable
    {
        private readonly IModel _channel;
        private readonly object _channelLock;
        private readonly string _consumerTag;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Func<int> _inFlightHandlersGetter;
        private bool _disposed;

        public RabbitMqConsumerSubscription(
            IModel channel,
            object channelLock,
            string consumerTag,
            CancellationTokenSource cancellationTokenSource,
            Func<int> inFlightHandlersGetter)
        {
            _channel = channel;
            _channelLock = channelLock;
            _consumerTag = consumerTag;
            _cancellationTokenSource = cancellationTokenSource;
            _inFlightHandlersGetter = inFlightHandlersGetter;
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
                try
                {
                    if (_channel.IsOpen)
                    {
                        // NoWait: o BasicCancel síncrono do RabbitMQ.Client 6.x pode lançar NullReferenceException
                        // quando o CancelOk chega depois de o consumer já ter saído da tabela interna.
                        _channel.BasicCancelNoWait(_consumerTag);
                    }
                }
                catch (Exception exception) when (exception is OperationInterruptedException or AlreadyClosedException)
                {
                    // O canal ou a conexão fecharam enquanto o serviço desligava: não há mais consumer a cancelar.
                }
            }

            var waitTimeout = TimeSpan.FromSeconds(5);
            var stopwatch = Stopwatch.StartNew();
            while (_inFlightHandlersGetter() > 0 && stopwatch.Elapsed < waitTimeout)
            {
                Thread.Sleep(50);
            }

            _channel.Dispose();
            _cancellationTokenSource.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

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
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;

    public RabbitMqCommandConsumer(IRabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public async Task StartConsumingAsync<TCommand>(
        string queueName,
        string routingKey,
        Func<MessageEnvelope<TCommand>, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        using var channel = _connectionProvider.CreateChannel();
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

                await handler(message, cancellationToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch
            {
                var retryCount = GetRetryCount(eventArgs.BasicProperties.Headers);
                if (retryCount < _options.MaxRetries)
                {
                    PublishToRetryExchange(channel, body, eventArgs.BasicProperties, routingKey, retryCount + 1);
                }
                else
                {
                    PublishToDeadLetterExchange(channel, body, eventArgs.BasicProperties, routingKey);
                }

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        };

        var consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = cancellationToken.Register(() =>
        {
            channel.BasicCancel(consumerTag);
            completion.TrySetResult();
        });

        await completion.Task;
    }

    private void DeclareTopology(IModel channel, string queueName, string routingKey)
    {
        var retryExchangeName = GetRetryExchangeName();
        var deadLetterExchangeName = GetDeadLetterExchangeName();
        var retryQueueName = $"{queueName}.retry";
        var deadLetterQueueName = $"{queueName}.dlq";

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

    private void PublishToRetryExchange(
        IModel channel,
        byte[] body,
        IBasicProperties properties,
        string routingKey,
        int retryCount)
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

    private void PublishToDeadLetterExchange(
        IModel channel,
        byte[] body,
        IBasicProperties properties,
        string routingKey)
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
}

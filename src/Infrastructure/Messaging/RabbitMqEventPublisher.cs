using System.Text;
using System.Text.Json;
using Application.Common.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly object _topologyLock = new();
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private bool _topologyEnsured;

    public RabbitMqEventPublisher(IRabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public Task PublishAsync<TEvent>(
        MessageEnvelope<TEvent> message,
        string routingKey,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var channel = _connectionProvider.CreateChannel();
        EnsureTopology(channel);

        var payload = JsonSerializer.Serialize(message, SerializerOptions);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.MessageId = message.MessageId.ToString();
        properties.CorrelationId = message.CorrelationId.ToString();
        properties.Type = message.EventType;
        properties.Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    private void EnsureTopology(IModel channel)
    {
        if (_topologyEnsured)
        {
            return;
        }

        lock (_topologyLock)
        {
            if (_topologyEnsured)
            {
                return;
            }

            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
            _topologyEnsured = true;
        }
    }
}

using System.Text;
using System.Text.Json;
using Application.Common.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly object _topologyLock = new();
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private bool _topologyEnsured;

    public RabbitMqEventPublisher(IRabbitMqConnectionProvider connectionProvider, IOptions<RabbitMqOptions> options)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
    }

    public Task PublishAsync(IntegrationMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        using var channel = _connectionProvider.CreateChannel();
        EnsureTopology(channel);

        var payload = JsonSerializer.Serialize(message, IntegrationMessage.SerializerOptions);
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 2;
        properties.MessageId = message.MessageId.ToString();
        properties.CorrelationId = message.CorrelationId;
        properties.Type = message.Type;
        properties.Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.SpecifyKind(message.OccurredAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: message.Type,
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

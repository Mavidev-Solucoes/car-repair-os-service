using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose()
    {
        _connection.Dispose();
    }
}

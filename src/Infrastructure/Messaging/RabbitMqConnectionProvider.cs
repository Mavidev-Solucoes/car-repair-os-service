using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IDisposable
{
    private readonly Lazy<IConnection> _connection;

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
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        // Conecta só no primeiro uso (com RabbitMq:Enabled=false nada abre conexão). PublicationOnly não guarda
        // a exceção: se o RabbitMQ estiver fora, a próxima chamada tenta de novo.
        _connection = new Lazy<IConnection>(() => factory.CreateConnection(), LazyThreadSafetyMode.PublicationOnly);
    }

    public IModel CreateChannel() => _connection.Value.CreateModel();

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            RabbitMqCommandConsumer.DisposeQuietly(_connection.Value);
        }
    }
}

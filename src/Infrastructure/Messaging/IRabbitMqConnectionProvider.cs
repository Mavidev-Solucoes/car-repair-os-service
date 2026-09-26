using RabbitMQ.Client;

namespace Infrastructure.Messaging;

public interface IRabbitMqConnectionProvider
{
    IModel CreateChannel();
}

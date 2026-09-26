namespace Application.Common.Messaging;

public interface ICommandConsumer
{
    Task<IAsyncDisposable> StartConsumingAsync<TCommand>(
        string queueName,
        string routingKey,
        Func<MessageEnvelope<TCommand>, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TCommand : class;
}

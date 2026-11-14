namespace Application.Common.Messaging;

public interface ICommandConsumer
{
    Task<IAsyncDisposable> StartConsumingAsync(
        string queueName,
        string commandType,
        Func<IntegrationMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}

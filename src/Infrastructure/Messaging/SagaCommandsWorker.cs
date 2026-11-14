using Application.Common.Messaging;
using Application.ServiceOrders.Saga;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

/// <summary>
/// Assina os comandos da saga destinados ao os-service (docs/contrato-mensageria.md).
/// </summary>
public sealed class SagaCommandsWorker(
    ICommandConsumer commandConsumer,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<SagaCommandsWorker> logger) : BackgroundService
{
    public const string QueuePrefix = "os-service.";
    public const string CloseServiceOrderCommandType = "CloseServiceOrderCommand";
    public const string CancelServiceOrderCommandType = "CancelServiceOrderCommand";

    private readonly List<IAsyncDisposable> _subscriptions = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("RabbitMQ is disabled; saga commands will not be consumed.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _subscriptions.Add(await commandConsumer.StartConsumingAsync(
                    QueuePrefix + CloseServiceOrderCommandType,
                    CloseServiceOrderCommandType,
                    (message, ct) => SendAsync(new CloseServiceOrderBySagaCommand(message.GetServiceOrderId()), ct),
                    stoppingToken));
                _subscriptions.Add(await commandConsumer.StartConsumingAsync(
                    QueuePrefix + CancelServiceOrderCommandType,
                    CancelServiceOrderCommandType,
                    (message, ct) => SendAsync(new CancelServiceOrderBySagaCommand(message.GetServiceOrderId()), ct),
                    stoppingToken));
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not subscribe to saga commands. Retrying in 5 seconds.");
                await DisposeSubscriptionsAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeSubscriptionsAsync();
        await base.StopAsync(cancellationToken);
    }

    public async Task SendAsync(IBaseRequest request, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(request, cancellationToken);
    }

    private async Task DisposeSubscriptionsAsync()
    {
        foreach (var subscription in _subscriptions)
        {
            await subscription.DisposeAsync();
        }

        _subscriptions.Clear();
    }
}

using System.Text.Json.Nodes;
using Application.Common.Messaging;
using Domain.Entities;
using Domain.Events;
using Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.ServiceOrders.Saga;

/// <summary>
/// Publica no RabbitMQ os eventos da OS que a saga e o rastreio precisam (docs/contrato-mensageria.md).
/// Roda depois do commit; falha de publicação é registrada em log e não desfaz a operação.
/// </summary>
public sealed class ServiceOrderIntegrationEventsHandler(
    IServiceOrderRepository serviceOrderRepository,
    ICustomerRepository customerRepository,
    IEventPublisher eventPublisher,
    ILogger<ServiceOrderIntegrationEventsHandler> logger)
    : INotificationHandler<ServiceOrderOpenedDomainEvent>,
      INotificationHandler<ServiceOrderClosedDomainEvent>,
      INotificationHandler<ServiceOrderCancelledDomainEvent>
{
    public const string ServiceOrderOpened = "ServiceOrderOpened";
    public const string ServiceOrderClosed = "ServiceOrderClosed";
    public const string ServiceOrderCancelled = "ServiceOrderCancelled";

    public async Task Handle(ServiceOrderOpenedDomainEvent notification, CancellationToken cancellationToken)
    {
        var serviceOrder = await serviceOrderRepository.GetByIdWithDetailsAsync(notification.ServiceOrderId, cancellationToken);
        if (serviceOrder is null)
        {
            logger.LogError("Service order {ServiceOrderId} not found to publish {EventType}.", notification.ServiceOrderId, ServiceOrderOpened);
            return;
        }

        var customer = await customerRepository.GetByIdAsync(serviceOrder.CustomerId, cancellationToken);
        await PublishAsync(ServiceOrderOpened, serviceOrder.Id, BuildOpenedPayload(serviceOrder, customer), cancellationToken);
    }

    public Task Handle(ServiceOrderClosedDomainEvent notification, CancellationToken cancellationToken)
    {
        return PublishAsync(ServiceOrderClosed, notification.ServiceOrderId, IdOnly(notification.ServiceOrderId), cancellationToken);
    }

    public Task Handle(ServiceOrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        return PublishAsync(ServiceOrderCancelled, notification.ServiceOrderId, IdOnly(notification.ServiceOrderId), cancellationToken);
    }

    public static JsonObject BuildOpenedPayload(ServiceOrder serviceOrder, Customer? customer)
    {
        var items = new JsonArray();
        foreach (var item in serviceOrder.ServiceItems)
        {
            items.Add(new JsonObject
            {
                ["description"] = item.Description,
                ["unitPrice"] = item.Price,
                ["quantity"] = item.Quantity
            });
        }

        return new JsonObject
        {
            ["serviceOrderId"] = serviceOrder.Id,
            ["customerId"] = serviceOrder.CustomerId,
            ["customerName"] = customer?.Name,
            ["vehicleId"] = serviceOrder.VehicleId,
            ["items"] = items,
            ["totalAmount"] = serviceOrder.TotalPrice
        };
    }

    private static JsonObject IdOnly(Guid serviceOrderId) => new() { ["serviceOrderId"] = serviceOrderId };

    private async Task PublishAsync(string type, Guid serviceOrderId, JsonObject payload, CancellationToken cancellationToken)
    {
        try
        {
            await eventPublisher.PublishAsync(
                new IntegrationMessage
                {
                    Type = type,
                    CorrelationId = serviceOrderId.ToString("D"),
                    OccurredAtUtc = DateTime.UtcNow,
                    Payload = payload
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish {EventType} for service order {ServiceOrderId}.", type, serviceOrderId);
        }
    }
}

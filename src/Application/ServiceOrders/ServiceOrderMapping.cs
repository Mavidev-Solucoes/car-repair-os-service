using Application.DTOs;
using Domain.Entities;

namespace Application.ServiceOrders;

internal static class ServiceOrderMapping
{
    public static ServiceOrderDto ToDto(ServiceOrder serviceOrder)
    {
        var items = serviceOrder.ServiceItems
            .Select(item => new ServiceOrderItemDto(
                item.Id,
                item.ServiceOrderId,
                item.ServiceItemId,
                item.Description,
                item.Price,
                item.Quantity))
            .ToList();

        var history = serviceOrder.StatusHistory
            .OrderBy(historyItem => historyItem.ChangedAt)
            .Select(historyItem => new ServiceStatusHistoryDto(
                historyItem.Id,
                historyItem.ServiceOrderId,
                historyItem.FromStatus?.ToString(),
                historyItem.ToStatus.ToString(),
                historyItem.ChangedAt,
                historyItem.ChangedByUserId))
            .ToList();

        return new ServiceOrderDto(
            serviceOrder.Id,
            serviceOrder.VehicleId,
            serviceOrder.CustomerId,
            serviceOrder.AssignedUserId,
            serviceOrder.Status.ToString(),
            serviceOrder.TotalPrice,
            serviceOrder.CreatedAt,
            serviceOrder.UpdatedAt,
            items,
            history);
    }
}

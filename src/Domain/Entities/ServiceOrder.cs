using Domain.Common;
using Domain.Enums;
using Domain.Events;

namespace Domain.Entities;

public class ServiceOrder : BaseEntity
{
    private readonly List<ServiceOrderItem> _items = [];
    private readonly List<ServiceStatusHistory> _statusHistory = [];

    private ServiceOrder()
    {
    }

    public ServiceOrder(Guid vehicleId, Guid customerId, Guid? createdUserId = null)
    {
        VehicleId = vehicleId;
        CustomerId = customerId;
        Status = ServiceOrderStatus.Received;
        SetCreatedBy(createdUserId);
        _statusHistory.Add(ServiceStatusHistory.CreateInitial(Id, createdUserId));
        RaiseDomainEvent(new ServiceOrderOpenedDomainEvent(Id, VehicleId, CustomerId));
    }

    public Guid VehicleId { get; private set; }
    public Guid CustomerId { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public Customer Customer { get; private set; } = null!;
    public IReadOnlyCollection<ServiceOrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<ServiceStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public ServiceOrderItem AddItem(string description, decimal unitPrice, int quantity, Guid? createdUserId = null)
    {
        EnsureCanChangeItems();

        var item = new ServiceOrderItem(Id, description, unitPrice, quantity, createdUserId);
        _items.Add(item);
        RecalculateTotal();

        if (Status == ServiceOrderStatus.Received)
        {
            UpdateStatus(ServiceOrderStatus.Diagnosing, createdUserId);
        }
        else
        {
            Touch(createdUserId);
        }

        RaiseDomainEvent(new ServiceOrderItemAddedDomainEvent(Id, item.Id));
        return item;
    }

    public void UpdateItem(Guid serviceOrderItemId, string description, decimal unitPrice, int quantity, Guid? updatedUserId = null)
    {
        EnsureCanChangeItems();

        var item = _items.FirstOrDefault(x => x.Id == serviceOrderItemId)
            ?? throw new InvalidOperationException("Service order item not found.");

        item.Update(description, unitPrice, quantity, updatedUserId);
        RecalculateTotal();
        Touch(updatedUserId);
    }

    public void RemoveItem(Guid serviceOrderItemId, Guid? updatedUserId = null)
    {
        EnsureCanChangeItems();

        var item = _items.FirstOrDefault(x => x.Id == serviceOrderItemId)
            ?? throw new InvalidOperationException("Service order item not found.");

        _items.Remove(item);
        RecalculateTotal();
        Touch(updatedUserId);
        RaiseDomainEvent(new ServiceOrderItemRemovedDomainEvent(Id, serviceOrderItemId));
    }

    public void UpdateStatus(ServiceOrderStatus newStatus, Guid? updatedUserId = null)
    {
        if (Status == newStatus)
        {
            return;
        }

        EnsureValidTransition(newStatus);

        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(ServiceStatusHistory.CreateTransition(Id, previousStatus, newStatus, updatedUserId));
        Touch(updatedUserId);
        RaiseDomainEvent(new ServiceOrderStatusChangedDomainEvent(Id, previousStatus, newStatus));
    }

    private void EnsureCanChangeItems()
    {
        if (Status is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Items can only be changed while the service order is active.");
        }
    }

    private void EnsureValidTransition(ServiceOrderStatus newStatus)
    {
        var isValid = Status switch
        {
            ServiceOrderStatus.Received => newStatus is ServiceOrderStatus.Diagnosing or ServiceOrderStatus.Cancelled,
            ServiceOrderStatus.Diagnosing => newStatus is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled,
            ServiceOrderStatus.Completed => false,
            ServiceOrderStatus.Cancelled => false,
            _ => false
        };

        if (!isValid)
        {
            throw new InvalidOperationException($"Cannot transition service order from {Status} to {newStatus}.");
        }
    }

    private void RecalculateTotal() => TotalAmount = _items.Sum(x => x.Total);
}

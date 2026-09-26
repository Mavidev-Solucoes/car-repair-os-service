using Domain.Common;
using Domain.Enums;
using Domain.Events;

namespace Domain.Entities;

public class ServiceOrder : BaseEntity
{
    private readonly List<ServiceOrderItem> _serviceItems = [];
    private readonly List<ServiceStatusHistory> _statusHistory = [];

    private ServiceOrder()
    {
    }

    public ServiceOrder(Guid vehicleId, Guid customerId, Guid assignedUserId)
    {
        VehicleId = vehicleId;
        CustomerId = customerId;
        AssignedUserId = assignedUserId;
        Status = ServiceStatus.Received;
        TotalPrice = 0m;

        SetCreatedBy(assignedUserId);
        _statusHistory.Add(ServiceStatusHistory.CreateInitial(Id, assignedUserId));
        RaiseDomainEvent(new ServiceOrderOpenedDomainEvent(Id));
    }

    public Guid VehicleId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid AssignedUserId { get; private set; }
    public ServiceStatus Status { get; private set; }
    public decimal TotalPrice { get; private set; }

    public IReadOnlyCollection<ServiceOrderItem> ServiceItems => _serviceItems.AsReadOnly();
    public IReadOnlyCollection<ServiceStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void AddServiceItem(ServiceOrderItem item, Guid requestingUserId)
    {
        EnsureCanModify(requestingUserId);
        _serviceItems.Add(item);

        RecalculateTotalPrice();
        AutoTransitionToDiagnosing(requestingUserId);
        Touch(requestingUserId);
    }

    public void Cancel(Guid requestingUserId)
    {
        if (Status == ServiceStatus.Cancelled)
        {
            throw new InvalidOperationException("Service order is already cancelled.");
        }

        if (Status == ServiceStatus.Closed)
        {
            throw new InvalidOperationException("Closed service orders cannot be cancelled.");
        }

        if (requestingUserId != AssignedUserId)
        {
            throw new InvalidOperationException("Only the assigned user can cancel this service order.");
        }

        TransitionToStatus(ServiceStatus.Cancelled, requestingUserId);
        Touch(requestingUserId);
        RaiseDomainEvent(new ServiceOrderCancelledDomainEvent(Id, requestingUserId));
    }

    public void Close(Guid requestingUserId)
    {
        if (Status == ServiceStatus.Closed)
        {
            throw new InvalidOperationException("Service order is already closed.");
        }

        if (Status == ServiceStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled service orders cannot be closed.");
        }

        if (requestingUserId != AssignedUserId)
        {
            throw new InvalidOperationException("Only the assigned user can close this service order.");
        }

        TransitionToStatus(ServiceStatus.Closed, requestingUserId);
        Touch(requestingUserId);
        RaiseDomainEvent(new ServiceOrderClosedDomainEvent(Id, requestingUserId));
    }

    private void EnsureCanModify(Guid requestingUserId)
    {
        if (Status is ServiceStatus.Cancelled or ServiceStatus.Closed)
        {
            throw new InvalidOperationException("Cancelled or closed service orders cannot be modified.");
        }

        if (requestingUserId != AssignedUserId)
        {
            throw new InvalidOperationException("Only the assigned user can modify this service order.");
        }
    }

    private void AutoTransitionToDiagnosing(Guid userId)
    {
        if (Status == ServiceStatus.Received)
        {
            TransitionToStatus(ServiceStatus.Diagnosing, userId);
        }
    }

    private void TransitionToStatus(ServiceStatus newStatus, Guid? userId)
    {
        var previousStatus = Status;
        Status = newStatus;
        _statusHistory.Add(ServiceStatusHistory.CreateTransition(Id, previousStatus, newStatus, userId));
    }

    private void RecalculateTotalPrice()
    {
        TotalPrice = _serviceItems.Sum(item => item.Price * item.Quantity);
    }
}

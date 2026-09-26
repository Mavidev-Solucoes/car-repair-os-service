using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class ServiceStatusHistory : BaseEntity
{
    private ServiceStatusHistory()
    {
    }

    private ServiceStatusHistory(Guid serviceOrderId, ServiceOrderStatus? fromStatus, ServiceOrderStatus toStatus, Guid? changedByUserId)
    {
        ServiceOrderId = serviceOrderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedAt = DateTime.UtcNow;
        ChangedByUserId = changedByUserId;
    }

    public Guid ServiceOrderId { get; private set; }
    public ServiceOrderStatus? FromStatus { get; private set; }
    public ServiceOrderStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public ServiceOrder ServiceOrder { get; private set; } = null!;

    internal static ServiceStatusHistory CreateInitial(Guid serviceOrderId, Guid? changedByUserId)
        => new(serviceOrderId, null, ServiceOrderStatus.Received, changedByUserId);

    internal static ServiceStatusHistory CreateTransition(Guid serviceOrderId, ServiceOrderStatus fromStatus, ServiceOrderStatus toStatus, Guid? changedByUserId)
        => new(serviceOrderId, fromStatus, toStatus, changedByUserId);
}

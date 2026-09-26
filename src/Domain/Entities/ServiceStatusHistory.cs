using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class ServiceStatusHistory : BaseEntity
{
    private ServiceStatusHistory()
    {
    }

    public Guid ServiceOrderId { get; private set; }
    public ServiceStatus? FromStatus { get; private set; }
    public ServiceStatus ToStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public static ServiceStatusHistory CreateInitial(Guid serviceOrderId, Guid? changedByUserId) =>
        new()
        {
            ServiceOrderId = serviceOrderId,
            ToStatus = ServiceStatus.Received,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };

    public static ServiceStatusHistory CreateTransition(Guid serviceOrderId, ServiceStatus fromStatus, ServiceStatus toStatus, Guid? changedByUserId) =>
        new()
        {
            ServiceOrderId = serviceOrderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
}

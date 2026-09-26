using Domain.Common;
using Domain.Enums;

namespace Domain.Events;

public sealed record ServiceOrderStatusChangedDomainEvent(Guid ServiceOrderId, ServiceOrderStatus PreviousStatus, ServiceOrderStatus CurrentStatus) : IDomainEvent;

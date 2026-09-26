using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderCancelledDomainEvent(Guid ServiceOrderId, Guid CancelledByUserId) : IDomainEvent;

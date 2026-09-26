using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderItemRemovedDomainEvent(Guid ServiceOrderId, Guid ServiceOrderItemId) : IDomainEvent;

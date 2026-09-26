using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderItemAddedDomainEvent(Guid ServiceOrderId, Guid ServiceOrderItemId) : IDomainEvent;

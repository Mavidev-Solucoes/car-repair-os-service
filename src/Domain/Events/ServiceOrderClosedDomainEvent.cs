using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderClosedDomainEvent(Guid ServiceOrderId, Guid ClosedByUserId) : IDomainEvent;

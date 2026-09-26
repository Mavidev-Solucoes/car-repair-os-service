using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderOpenedDomainEvent(Guid ServiceOrderId) : IDomainEvent;

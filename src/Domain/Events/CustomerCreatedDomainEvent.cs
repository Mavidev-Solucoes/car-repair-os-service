using Domain.Common;

namespace Domain.Events;

public sealed record CustomerCreatedDomainEvent(Guid CustomerId, string PersonalId) : IDomainEvent;

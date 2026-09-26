using Domain.Common;

namespace Domain.Events;

public sealed record ServiceOrderOpenedDomainEvent(Guid ServiceOrderId, Guid VehicleId, Guid CustomerId) : IDomainEvent;

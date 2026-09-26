using Domain.Common;

namespace Domain.Events;

public sealed record VehicleCreatedDomainEvent(Guid VehicleId, Guid CustomerId, string LicensePlate) : IDomainEvent;

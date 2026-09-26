using Application.DTOs;
using MediatR;

namespace Application.ServiceOrders.Commands;

public record CreateServiceOrderItemInput(Guid ServiceItemId, string Description, decimal Price, int Quantity);

public record CreateServiceOrderCommand(
    Guid VehicleId,
    Guid CustomerId,
    IEnumerable<CreateServiceOrderItemInput>? Items = null) : IRequest<ServiceOrderDto>;

public record CancelServiceOrderCommand(Guid Id) : IRequest<ServiceOrderDto>;

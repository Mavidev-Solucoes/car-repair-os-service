using Application.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.ServiceOrders.Commands;

public record OpenServiceOrderCommand(Guid VehicleId, Guid CustomerId) : IRequest<ServiceOrderDto>;

public record AddServiceOrderItemCommand(Guid ServiceOrderId, string Description, decimal UnitPrice, int Quantity) : IRequest<ServiceOrderDto>;

public record UpdateServiceOrderItemCommand(Guid ServiceOrderId, Guid ServiceOrderItemId, string Description, decimal UnitPrice, int Quantity) : IRequest<ServiceOrderDto>;

public record RemoveServiceOrderItemCommand(Guid ServiceOrderId, Guid ServiceOrderItemId) : IRequest<ServiceOrderDto>;

public record UpdateServiceOrderStatusCommand(Guid ServiceOrderId, ServiceOrderStatus Status) : IRequest<ServiceOrderDto>;

public record DeleteServiceOrderCommand(Guid Id) : IRequest<Unit>;

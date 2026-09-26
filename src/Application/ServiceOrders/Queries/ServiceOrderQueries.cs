using Application.Common;
using Application.DTOs;
using Domain.Enums;
using MediatR;

namespace Application.ServiceOrders.Queries;

public record GetServiceOrderByIdQuery(Guid Id) : IRequest<ServiceOrderDto>;

public record GetServiceStatusHistoryQuery(Guid ServiceOrderId) : IRequest<IEnumerable<ServiceStatusHistoryDto>>;

public record GetServiceOrdersQuery : IRequest<PagedResult<ServiceOrderDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? VehicleId { get; init; }
    public ServiceOrderStatus? Status { get; init; }
}

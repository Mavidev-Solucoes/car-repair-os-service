using Application.ServiceOrders;
using System.Linq.Expressions;
using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.ServiceOrders.Queries;

public class GetServiceOrderByIdQueryHandler : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetServiceOrderByIdQueryHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<ServiceOrderDto> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _serviceOrderRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        return ServiceOrderMapping.ToDto(order);
    }
}

public class GetServiceOrdersQueryHandler : IRequestHandler<GetServiceOrdersQuery, PagedResult<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;

    public GetServiceOrdersQueryHandler(IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<PagedResult<ServiceOrderDto>> Handle(GetServiceOrdersQuery request, CancellationToken cancellationToken)
    {
        var filters = BuildFilters(request);

        var (items, totalCount) = await _serviceOrderRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        var dtos = items.Select(ServiceOrderMapping.ToDto);
        return new PagedResult<ServiceOrderDto>(dtos, totalCount, request.Page, request.PageSize);
    }

    private static IEnumerable<Expression<Func<ServiceOrder, bool>>> BuildFilters(GetServiceOrdersQuery request)
    {
        var filters = new List<Expression<Func<ServiceOrder, bool>>>();

        if (request.CustomerId.HasValue)
        {
            filters.Add(order => order.CustomerId == request.CustomerId.Value);
        }

        if (request.VehicleId.HasValue)
        {
            filters.Add(order => order.VehicleId == request.VehicleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ServiceStatus>(request.Status, true, out var status))
        {
            filters.Add(order => order.Status == status);
        }

        return filters;
    }
}

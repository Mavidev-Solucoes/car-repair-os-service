using System.Linq.Expressions;
using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.ServiceOrders.Queries;

public class GetServiceOrderByIdQueryHandler : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IMapper _mapper;

    public GetServiceOrderByIdQueryHandler(IServiceOrderRepository serviceOrderRepository, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        return _mapper.Map<ServiceOrderDto>(serviceOrder);
    }
}

public class GetServiceStatusHistoryQueryHandler : IRequestHandler<GetServiceStatusHistoryQuery, IEnumerable<ServiceStatusHistoryDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IMapper _mapper;

    public GetServiceStatusHistoryQueryHandler(IServiceOrderRepository serviceOrderRepository, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ServiceStatusHistoryDto>> Handle(GetServiceStatusHistoryQuery request, CancellationToken cancellationToken)
    {
        _ = await _serviceOrderRepository.GetByIdAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        var history = await _serviceOrderRepository.GetStatusHistoryAsync(request.ServiceOrderId, cancellationToken);
        return _mapper.Map<IEnumerable<ServiceStatusHistoryDto>>(history);
    }
}

public class GetServiceOrdersQueryHandler : IRequestHandler<GetServiceOrdersQuery, PagedResult<ServiceOrderDto>>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IMapper _mapper;

    public GetServiceOrdersQueryHandler(IServiceOrderRepository serviceOrderRepository, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ServiceOrderDto>> Handle(GetServiceOrdersQuery request, CancellationToken cancellationToken)
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

        if (request.Status.HasValue)
        {
            filters.Add(order => order.Status == request.Status.Value);
        }

        var (items, totalCount) = await _serviceOrderRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<ServiceOrderDto>(_mapper.Map<IEnumerable<ServiceOrderDto>>(items), totalCount, request.Page, request.PageSize);
    }
}

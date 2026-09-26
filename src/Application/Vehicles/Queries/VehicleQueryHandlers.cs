using System.Linq.Expressions;
using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Vehicles.Queries;

public class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetVehicleByIdQueryHandler(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        return _mapper.Map<VehicleDto>(vehicle);
    }
}

public class GetVehiclesByCustomerIdQueryHandler : IRequestHandler<GetVehiclesByCustomerIdQuery, IEnumerable<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetVehiclesByCustomerIdQueryHandler(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VehicleDto>> Handle(GetVehiclesByCustomerIdQuery request, CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }
}

public class GetVehiclesQueryHandler : IRequestHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetVehiclesQueryHandler(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var filters = new List<Expression<Func<Vehicle, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            filters.Add(vehicle => vehicle.Brand.Contains(request.Brand));
        }

        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            filters.Add(vehicle => vehicle.Model.Contains(request.Model));
        }

        if (request.Year.HasValue)
        {
            filters.Add(vehicle => vehicle.Year == request.Year.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.LicensePlate))
        {
            var normalized = new string(request.LicensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            filters.Add(vehicle => vehicle.LicensePlate.Contains(normalized));
        }

        var (items, totalCount) = await _vehicleRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<VehicleDto>(_mapper.Map<IEnumerable<VehicleDto>>(items), totalCount, request.Page, request.PageSize);
    }
}

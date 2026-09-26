using Application.Common.Exceptions;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Vehicles.Commands;

public class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateVehicleCommandHandler(IVehicleRepository vehicleRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VehicleDto> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        _ = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        if (await _vehicleRepository.ExistsByLicensePlateAsync(request.LicensePlate, cancellationToken))
        {
            throw new BusinessException($"A vehicle with license plate '{request.LicensePlate}' already exists.");
        }

        var vehicle = new Vehicle(request.CustomerId, request.Brand, request.Model, request.Year, request.LicensePlate, request.Color);
        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<VehicleDto>(vehicle);
    }
}

public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, VehicleDto>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateVehicleCommandHandler(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        var normalizedPlate = new string(request.LicensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (!string.Equals(vehicle.LicensePlate, normalizedPlate, StringComparison.OrdinalIgnoreCase) &&
            await _vehicleRepository.ExistsByLicensePlateAsync(request.LicensePlate, cancellationToken))
        {
            throw new BusinessException($"A vehicle with license plate '{request.LicensePlate}' already exists.");
        }

        vehicle.Update(request.Brand, request.Model, request.Year, request.LicensePlate, request.Color);
        _vehicleRepository.Update(vehicle);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<VehicleDto>(vehicle);
    }
}

public class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand, Unit>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVehicleCommandHandler(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.Id);

        if (await _vehicleRepository.HasServiceOrdersAsync(request.Id, cancellationToken))
        {
            throw new BusinessException("This vehicle cannot be deleted because it has related service orders.");
        }

        _vehicleRepository.Delete(vehicle);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}

using Application.ServiceOrders;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.ServiceOrders.Commands;

public class CreateServiceOrderCommandHandler : IRequestHandler<CreateServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateServiceOrderCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var assignedUserId = _currentUserService.UserId
            ?? throw new BusinessException("Header 'X-User-Id' is required to create a service order.");

        _ = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.CustomerId != request.CustomerId)
        {
            throw new BusinessException("Vehicle does not belong to the informed customer.");
        }

        var serviceOrder = new ServiceOrder(request.VehicleId, request.CustomerId, assignedUserId);

        if (request.Items is not null)
        {
            foreach (var itemInput in request.Items)
            {
                var item = new ServiceOrderItem(
                    serviceOrder.Id,
                    itemInput.ServiceItemId,
                    itemInput.Description,
                    itemInput.Price,
                    itemInput.Quantity,
                    assignedUserId);

                serviceOrder.AddServiceItem(item, assignedUserId);
            }
        }

        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return ServiceOrderMapping.ToDto(serviceOrder);
    }
}

public class CancelServiceOrderCommandHandler : IRequestHandler<CancelServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CancelServiceOrderCommandHandler(
        IServiceOrderRepository serviceOrderRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceOrderDto> Handle(CancelServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new BusinessException("Header 'X-User-Id' is required to cancel a service order.");

        var serviceOrder = await _serviceOrderRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        serviceOrder.Cancel(userId);
        _serviceOrderRepository.Update(serviceOrder);

        await _unitOfWork.CommitAsync(cancellationToken);
        return ServiceOrderMapping.ToDto(serviceOrder);
    }
}

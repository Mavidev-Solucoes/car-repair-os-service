using Application.Common.Exceptions;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.ServiceOrders.Commands;

public class OpenServiceOrderCommandHandler : IRequestHandler<OpenServiceOrderCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OpenServiceOrderCommandHandler(IServiceOrderRepository serviceOrderRepository, ICustomerRepository customerRepository, IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(OpenServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (vehicle.CustomerId != customer.Id)
        {
            throw new BusinessException("The vehicle does not belong to the informed customer.");
        }

        var serviceOrder = new ServiceOrder(request.VehicleId, request.CustomerId);
        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        var detailed = await _serviceOrderRepository.GetWithDetailsAsync(serviceOrder.Id, cancellationToken) ?? serviceOrder;
        return _mapper.Map<ServiceOrderDto>(detailed);
    }
}

public class AddServiceOrderItemCommandHandler : IRequestHandler<AddServiceOrderItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddServiceOrderItemCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(AddServiceOrderItemCommand request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        serviceOrder.AddItem(request.Description, request.UnitPrice, request.Quantity);
        _serviceOrderRepository.Update(serviceOrder);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<ServiceOrderDto>(serviceOrder);
    }
}

public class UpdateServiceOrderItemCommandHandler : IRequestHandler<UpdateServiceOrderItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateServiceOrderItemCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(UpdateServiceOrderItemCommand request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        serviceOrder.UpdateItem(request.ServiceOrderItemId, request.Description, request.UnitPrice, request.Quantity);
        _serviceOrderRepository.Update(serviceOrder);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<ServiceOrderDto>(serviceOrder);
    }
}

public class RemoveServiceOrderItemCommandHandler : IRequestHandler<RemoveServiceOrderItemCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RemoveServiceOrderItemCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(RemoveServiceOrderItemCommand request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        serviceOrder.RemoveItem(request.ServiceOrderItemId);
        _serviceOrderRepository.Update(serviceOrder);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<ServiceOrderDto>(serviceOrder);
    }
}

public class UpdateServiceOrderStatusCommandHandler : IRequestHandler<UpdateServiceOrderStatusCommand, ServiceOrderDto>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateServiceOrderStatusCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceOrderDto> Handle(UpdateServiceOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetWithDetailsAsync(request.ServiceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.ServiceOrderId);

        serviceOrder.UpdateStatus(request.Status);
        _serviceOrderRepository.Update(serviceOrder);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<ServiceOrderDto>(serviceOrder);
    }
}

public class DeleteServiceOrderCommandHandler : IRequestHandler<DeleteServiceOrderCommand, Unit>
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteServiceOrderCommandHandler(IServiceOrderRepository serviceOrderRepository, IUnitOfWork unitOfWork)
    {
        _serviceOrderRepository = serviceOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteServiceOrderCommand request, CancellationToken cancellationToken)
    {
        var serviceOrder = await _serviceOrderRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceOrder), request.Id);

        _serviceOrderRepository.Delete(serviceOrder);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}

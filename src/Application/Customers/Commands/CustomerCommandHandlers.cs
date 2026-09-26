using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Customers.Commands;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (await _customerRepository.ExistsByPersonalIdAsync(request.PersonalId, cancellationToken))
        {
            throw new BusinessException($"A customer with personal ID '{request.PersonalId}' already exists.");
        }

        if (await _customerRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessException($"A customer with email '{request.Email}' already exists.");
        }

        var customer = new Customer(request.Name, request.PersonalId, request.Email, request.Telephone, _currentUserService.UserId);
        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<CustomerDto>(customer);
    }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        if (!string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await _customerRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessException($"A customer with email '{request.Email}' already exists.");
        }

        customer.Update(request.Name, request.Email, request.Telephone, request.IsActive, _currentUserService.UserId);

        _customerRepository.Update(customer);
        await _unitOfWork.CommitAsync(cancellationToken);
        return _mapper.Map<CustomerDto>(customer);
    }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Unit>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        if (await _customerRepository.HasServiceOrdersAsync(request.Id, cancellationToken))
        {
            throw new BusinessException("This customer cannot be deleted because it has related service orders.");
        }

        if (await _customerRepository.HasVehiclesAsync(request.Id, cancellationToken))
        {
            throw new BusinessException("This customer cannot be deleted because it still has registered vehicles.");
        }

        _customerRepository.Delete(customer);
        await _unitOfWork.CommitAsync(cancellationToken);
        return Unit.Value;
    }
}

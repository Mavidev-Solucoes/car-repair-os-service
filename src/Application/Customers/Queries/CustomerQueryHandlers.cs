using System.Linq.Expressions;
using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Customers.Queries;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        return _mapper.Map<CustomerDto>(customer);
    }
}

public class GetCustomerWithVehiclesQueryHandler : IRequestHandler<GetCustomerWithVehiclesQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomerWithVehiclesQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<CustomerDto> Handle(GetCustomerWithVehiclesQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetWithVehiclesAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        return _mapper.Map<CustomerDto>(customer);
    }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var filters = new List<Expression<Func<Customer, bool>>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            filters.Add(customer => customer.Name.Contains(request.Name));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            filters.Add(customer => customer.Email.Contains(request.Email));
        }

        if (!string.IsNullOrWhiteSpace(request.PersonalId))
        {
            var normalized = new string(request.PersonalId.Where(char.IsDigit).ToArray());
            filters.Add(customer => customer.PersonalId.Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(request.Telephone))
        {
            var normalized = new string(request.Telephone.Where(char.IsDigit).ToArray());
            filters.Add(customer => customer.Telephone.Contains(normalized));
        }

        var (items, totalCount) = await _customerRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.OrderBy,
            request.OrderDescending,
            filters,
            cancellationToken);

        return new PagedResult<CustomerDto>(_mapper.Map<IEnumerable<CustomerDto>>(items), totalCount, request.Page, request.PageSize);
    }
}

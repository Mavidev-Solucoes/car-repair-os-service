using Application.DTOs;
using MediatR;

namespace Application.Customers.Commands;

public record CreateCustomerCommand(string Name, string PersonalId, string Email, string Telephone) : IRequest<CustomerDto>;

public record UpdateCustomerCommand(Guid Id, string Name, string Email, string Telephone, bool IsActive) : IRequest<CustomerDto>;

public record DeleteCustomerCommand(Guid Id) : IRequest<Unit>;

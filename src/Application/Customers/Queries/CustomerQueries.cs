using Application.Common;
using Application.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public record GetCustomersQuery : IRequest<PagedResult<CustomerDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? PersonalId { get; init; }
    public string? Telephone { get; init; }
}

public class GetCustomersQueryValidator : AbstractValidator<GetCustomersQuery>
{
    public GetCustomersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("PageSize must be between 1 and 200.");
    }
}

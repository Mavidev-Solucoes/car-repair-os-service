using Application.Common;
using Application.DTOs;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.ServiceOrders.Queries;

public record GetServiceOrderByIdQuery(Guid Id) : IRequest<ServiceOrderDto>;

public record GetServiceOrdersQuery : IRequest<PagedResult<ServiceOrderDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? VehicleId { get; init; }
    public string? Status { get; init; }
}

public class GetServiceOrdersQueryValidator : AbstractValidator<GetServiceOrdersQuery>
{
    public GetServiceOrdersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("PageSize must be between 1 and 200.");

        RuleFor(query => query.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || Enum.TryParse<ServiceStatus>(status, true, out _))
            .WithMessage("Status must match a valid service order status.");
    }
}

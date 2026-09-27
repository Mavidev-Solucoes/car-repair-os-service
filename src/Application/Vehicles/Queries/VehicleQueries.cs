using Application.Common;
using Application.DTOs;
using FluentValidation;
using MediatR;

namespace Application.Vehicles.Queries;

public record GetVehicleByIdQuery(Guid Id) : IRequest<VehicleDto>;

public record GetVehiclesQuery : IRequest<PagedResult<VehicleDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool OrderDescending { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public string? LicensePlate { get; init; }
}

public record GetVehiclesByCustomerIdQuery(Guid CustomerId) : IRequest<IEnumerable<VehicleDto>>;

public class GetVehiclesQueryValidator : AbstractValidator<GetVehiclesQuery>
{
    public GetVehiclesQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0).WithMessage("Page must be greater than zero.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 200).WithMessage("PageSize must be between 1 and 200.");
    }
}

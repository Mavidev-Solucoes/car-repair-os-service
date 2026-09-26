using Application.Common.Validators;
using FluentValidation;

namespace Application.Vehicles.Commands;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.LicensePlate).NotEmpty().Must(BrazilianLicensePlateValidator.IsValid).WithMessage("License plate must follow the Brazilian Mercosul format.");
        RuleFor(x => x.Color).MaximumLength(50).When(x => x.Color is not null);
    }
}

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.LicensePlate).NotEmpty().Must(BrazilianLicensePlateValidator.IsValid).WithMessage("License plate must follow the Brazilian Mercosul format.");
        RuleFor(x => x.Color).MaximumLength(50).When(x => x.Color is not null);
    }
}

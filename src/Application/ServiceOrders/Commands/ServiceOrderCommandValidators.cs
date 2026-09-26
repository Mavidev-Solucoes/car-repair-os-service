using FluentValidation;

namespace Application.ServiceOrders.Commands;

public class CreateServiceOrderCommandValidator : AbstractValidator<CreateServiceOrderCommand>
{
    public CreateServiceOrderCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ServiceItemId)
                    .NotEmpty().WithMessage("Service item ID is required.");

                item.RuleFor(i => i.Description)
                    .NotEmpty().WithMessage("Description is required.")
                    .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");

                item.RuleFor(i => i.Price)
                    .GreaterThan(0).WithMessage("Price must be greater than zero.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
            });
    }
}

public class CancelServiceOrderCommandValidator : AbstractValidator<CancelServiceOrderCommand>
{
    public CancelServiceOrderCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Service order ID is required.");
    }
}

using Domain.Enums;
using FluentValidation;

namespace Application.ServiceOrders.Commands;

public class OpenServiceOrderCommandValidator : AbstractValidator<OpenServiceOrderCommand>
{
    public OpenServiceOrderCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class AddServiceOrderItemCommandValidator : AbstractValidator<AddServiceOrderItemCommand>
{
    public AddServiceOrderItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class UpdateServiceOrderItemCommandValidator : AbstractValidator<UpdateServiceOrderItemCommand>
{
    public UpdateServiceOrderItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.ServiceOrderItemId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class RemoveServiceOrderItemCommandValidator : AbstractValidator<RemoveServiceOrderItemCommand>
{
    public RemoveServiceOrderItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.ServiceOrderItemId).NotEmpty();
    }
}

public class UpdateServiceOrderStatusCommandValidator : AbstractValidator<UpdateServiceOrderStatusCommand>
{
    public UpdateServiceOrderStatusCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Status).NotEqual(ServiceOrderStatus.Received).WithMessage("Use order creation to create a received service order.");
    }
}

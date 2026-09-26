using Application.Common.Validators;
using FluentValidation;

namespace Application.Customers.Commands;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => name.Trim().Contains(' ')).WithMessage("Name must include both a first name and a last name.");

        RuleFor(x => x.PersonalId)
            .NotEmpty()
            .Must(BrazilianDocumentValidator.IsValidCpfOrCnpj).WithMessage("PersonalId must contain 11 or 14 digits.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);

        RuleFor(x => x.Telephone)
            .NotEmpty()
            .Must(phone => new string(phone.Where(char.IsDigit).ToArray()).Length is 10 or 11)
            .WithMessage("Telephone must contain 10 or 11 digits.");
    }
}

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => name.Trim().Contains(' ')).WithMessage("Name must include both a first name and a last name.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Telephone)
            .NotEmpty()
            .Must(phone => new string(phone.Where(char.IsDigit).ToArray()).Length is 10 or 11)
            .WithMessage("Telephone must contain 10 or 11 digits.");
    }
}

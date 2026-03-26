using FluentValidation;
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Name can only contain letters and spaces.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone must be exactly 10 digits.")
            .Must(p => !p.StartsWith("0")).WithMessage("Phone number cannot start with 0.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => r == "ADMIN" || r == "CUSTOMER")
            .WithMessage("Role must be either ADMIN or CUSTOMER.");
    }
}
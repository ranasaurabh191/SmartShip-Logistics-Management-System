using FluentValidation;
using SmartShip.IdentityService.DTOs;

namespace SmartShip.IdentityService.Validators;

public class SignupOtpRequestValidator : AbstractValidator<SignupOtpRequest>
{
    public SignupOtpRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Name can only contain letters and spaces.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required (+91xxxxxxxxx).")
            .Matches(@"^\+91[6-9]\d{9}$").WithMessage("Phone must be +91 followed by 10 digits (6-9 start).");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase.")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase.")
            .Matches(@"\d").WithMessage("Password must contain digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain special char.");
    }
}

public class VerifyOtpRequestValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Length(6).WithMessage("OTP must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP must contain only digits.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MinimumLength(2).MaximumLength(100)
            .Matches(@"^[a-zA-Z\s]+$");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required (+91xxxxxxxxx).")
            .Matches(@"^\+91[6-9]\d{9}$");

        RuleFor(x => x.Password)
            .MinimumLength(8)
            .Matches(@"[A-Z]").Matches(@"[a-z]").Matches(@"\d").Matches(@"[\W_]");
    }
}
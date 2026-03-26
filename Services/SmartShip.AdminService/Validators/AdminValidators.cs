using FluentValidation;
using SmartShip.AdminService.DTOs;

namespace SmartShip.AdminService.Validators;

public class CreateHubRequestValidator : AbstractValidator<CreateHubRequest>
{
    public CreateHubRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hub name is required.")
            .MinimumLength(3).WithMessage("Hub name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Hub name cannot exceed 100 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("City can only contain letters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("State can only contain letters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Contact phone is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone must be exactly 10 digits.");
    }
}

public class UpdateHubRequestValidator : AbstractValidator<UpdateHubRequest>
{
    public UpdateHubRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hub name is required.")
            .MinimumLength(3).WithMessage("Hub name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Hub name cannot exceed 100 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("City can only contain letters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("Contact phone is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone must be exactly 10 digits.");
    }
}

public class ReportRequestValidator : AbstractValidator<ReportRequest>
{
    private readonly string[] _validTypes = { "Operational", "Performance", "SLA", "Delivery" };

    public ReportRequestValidator()
    {
        RuleFor(x => x.ReportType)
            .NotEmpty().WithMessage("Report type is required.")
            .Must(t => _validTypes.Contains(t))
            .WithMessage("Report type must be: Operational, Performance, SLA, or Delivery.");

        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("From date is required.")
            .LessThan(x => x.ToDate).WithMessage("From date must be before To date.");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("To date is required.")
            .GreaterThan(x => x.FromDate).WithMessage("To date must be after From date.");
    }
}
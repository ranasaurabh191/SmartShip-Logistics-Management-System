using FluentValidation;
using SmartShip.ShipmentService.DTOs;

namespace SmartShip.ShipmentService.Validators;

public class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone must be exactly 10 digits.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required.")
            .MaximumLength(200).WithMessage("Street cannot exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("City can only contain letters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100);
    }
}

public class PackageValidator : AbstractValidator<PackageDto>
{
    public PackageValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(500).WithMessage("Weight cannot exceed 500 kg.");

        RuleFor(x => x.LengthCm)
            .GreaterThan(0).WithMessage("Length must be greater than 0.")
            .LessThanOrEqualTo(300).WithMessage("Length cannot exceed 300 cm.");

        RuleFor(x => x.WidthCm)
            .GreaterThan(0).WithMessage("Width must be greater than 0.")
            .LessThanOrEqualTo(300).WithMessage("Width cannot exceed 300 cm.");

        RuleFor(x => x.HeightCm)
            .GreaterThan(0).WithMessage("Height must be greater than 0.")
            .LessThanOrEqualTo(300).WithMessage("Height cannot exceed 300 cm.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("Declared value cannot be negative.")
            .LessThanOrEqualTo(10000000).WithMessage("Declared value cannot exceed 1 crore.");
    }
}

public class CreateShipmentRequestValidator : AbstractValidator<CreateShipmentRequest>
{
    public CreateShipmentRequestValidator()
    {
        RuleFor(x => x.SenderAddress)
            .NotNull().WithMessage("Sender address is required.")
            .SetValidator(new AddressValidator());

        RuleFor(x => x.ReceiverAddress)
            .NotNull().WithMessage("Receiver address is required.")
            .SetValidator(new AddressValidator());

        RuleFor(x => x.Package)
            .NotNull().WithMessage("Package details are required.")
            .SetValidator(new PackageValidator());

        RuleFor(x => x.ShipmentType)
            .IsInEnum().WithMessage("Invalid shipment type.");

        RuleFor(x => x.PickupScheduledAt)
            .GreaterThan(DateTime.Now).WithMessage("Pickup time must be in the future.")
            .When(x => x.PickupScheduledAt.HasValue);
    }
}

public class SchedulePickupRequestValidator : AbstractValidator<SchedulePickupRequest>
{
    public SchedulePickupRequestValidator()
    {
        RuleFor(x => x.PickupTime)  
            .GreaterThan(DateTime.Now)
            .WithMessage("Pickup time must be in the future.");

    }
}
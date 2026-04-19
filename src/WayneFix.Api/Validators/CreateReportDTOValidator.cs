using FluentValidation;
using WayneFix.Api.Contracts;

namespace WayneFix.Api.Validators;

public class CreateReportDTOValidator : AbstractValidator<CreateReportDTO>
{
    public CreateReportDTOValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Report text is required.")
            .MinimumLength(10)
            .WithMessage("Report text must be at least 10 characters.")
            .MaximumLength(10_000)
            .WithMessage("Report text cannot exceed 10.000 characters.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required.")
            .MaximumLength(200)
            .WithMessage("Location cannot exceed 200 characters.");

        RuleFor(x => x.Recipients)
            .NotEmpty()
            .WithMessage("At least one recipient is required.")
            .Must(r => r.Count <= 50)
            .WithMessage("Cannot exceed 50 recipients.");

        RuleForEach(x => x.Recipients)
            .NotEmpty()
            .WithMessage("Recipient cannot be an empty string.")
            .EmailAddress()
            .WithMessage("Each recipient must be a valid email address.");
    }
}

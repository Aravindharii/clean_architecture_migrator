using FluentValidation;
using LibraryManagement.API.DTOs.Copies;

namespace LibraryManagement.API.Validators.Copies;

/// <summary>
/// Validates data required to update a physical book copy.
/// </summary>
public class CopyUpdateValidator : AbstractValidator<CopyUpdateDto>
{
    /// <summary>
    /// Initializes validation rules for copy updates.
    /// </summary>
    public CopyUpdateValidator()
    {
        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required.")
            .MaximumLength(100)
            .WithMessage("Location cannot exceed 100 characters.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(status =>
                status == "Available" ||
                status == "OnLoan" ||
                status == "Damaged" ||
                status == "Retired")
            .WithMessage(
                "Status must be Available, OnLoan, Damaged, or Retired.");
    }
}
using FluentValidation;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

/// <summary>
/// Validates data required to create a physical book copy.
/// </summary>
public class CopyCreateValidator : AbstractValidator<CopyCreateDto>
{
    /// <summary>
    /// Initializes validation rules for copy creation.
    /// </summary>
    public CopyCreateValidator()
    {
        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required.")
            .MaximumLength(100)
            .WithMessage("Location cannot exceed 100 characters.");
    }
}
using FluentValidation;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

/// <summary>
/// Validates user login requests.
/// </summary>
public class LoginValidator : AbstractValidator<LoginDto>
{
    /// <summary>
    /// Initializes validation rules for user login.
    /// </summary>
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100)
            .WithMessage("Password cannot exceed 100 characters.");
    }
}
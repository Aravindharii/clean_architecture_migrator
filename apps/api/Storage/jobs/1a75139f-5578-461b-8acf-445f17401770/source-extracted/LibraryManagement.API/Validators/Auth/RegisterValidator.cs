using FluentValidation;
using LibraryManagement.API.DTOs.Auth;

namespace LibraryManagement.API.Validators.Auth;

/// <summary>
/// Validates user registration requests.
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    /// <summary>
    /// Initializes validation rules for user registration.
    /// </summary>
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("First name cannot contain only whitespace.")
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Last name cannot contain only whitespace.")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters.");

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
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one special character.");
    }
}
using FluentValidation;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .MustAsync(async (email, cancellation) =>
            {
                var exists = await userRepository.ExistsByEmailAsync(email!);
                return !exists;
            }).WithMessage("A user with this email already exists.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Role is required.")
            .Must(IsValidRole).WithMessage("Role must be one of: admin, librarian, member.");
    }

    private static bool IsValidRole(string role)
    {
        return Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed);
    }
}

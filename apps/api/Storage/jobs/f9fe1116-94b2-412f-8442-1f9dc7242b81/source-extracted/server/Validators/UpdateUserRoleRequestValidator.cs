using FluentValidation;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Validators;

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
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

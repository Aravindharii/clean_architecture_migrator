using FluentValidation;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

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

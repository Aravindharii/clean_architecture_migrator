using FluentValidation;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class AuthorCreateRequestValidator : AbstractValidator<AuthorCreateValidator>
{
    public AuthorCreateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Author name is required.")
            .MaximumLength(200).WithMessage("Author name must not exceed 200 characters.");
    }
}

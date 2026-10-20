using FluentValidation;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class AuthorUpdateRequestValidator : AbstractValidator<AuthorUpdateValidator>
{
    public AuthorUpdateRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Author name is required.")
            .MaximumLength(200).WithMessage("Author name must not exceed 200 characters.");
    }
}

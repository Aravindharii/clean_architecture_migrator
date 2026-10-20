using FluentValidation;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class CopyCreateRequestValidator : AbstractValidator<CopyCreateValidator>
{
    public CopyCreateRequestValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("BookId is required.");

        RuleFor(x => x.Barcode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Barcode is required.")
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters.");
    }
}

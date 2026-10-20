using FluentValidation;
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Validators;

public class CopyUpdateRequestValidator : AbstractValidator<CopyUpdateValidator>
{
    public CopyUpdateRequestValidator()
    {
        RuleFor(x => x.Barcode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Barcode is required.")
            .MaximumLength(50).WithMessage("Barcode must not exceed 50 characters.");

        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.");
    }
}

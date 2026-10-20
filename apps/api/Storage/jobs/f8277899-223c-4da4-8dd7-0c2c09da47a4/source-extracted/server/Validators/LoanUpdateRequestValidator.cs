using FluentValidation;
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Validators;

public class LoanUpdateRequestValidator : AbstractValidator<LoanUpdateValidator>
{
    public LoanUpdateRequestValidator()
    {
        RuleFor(x => x.CopyId)
            .GreaterThan(0).WithMessage("CopyId is required.");

        RuleFor(x => x.BorrowerName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Borrower name is required.")
            .MaximumLength(200).WithMessage("Borrower name must not exceed 200 characters.");

        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(50).WithMessage("Status must not exceed 50 characters.");

        RuleFor(x => x.DueDate)
            .GreaterThan(x => x.IssueDate).WithMessage("Due date must be after issue date.");
    }
}

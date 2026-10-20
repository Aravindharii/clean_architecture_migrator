using FluentValidation;
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Validators;

public class LoanCreateRequestValidator : AbstractValidator<LoanCreateValidator>
{
    public LoanCreateRequestValidator()
    {
        RuleFor(x => x.CopyId)
            .GreaterThan(0).WithMessage("CopyId is required.");

        RuleFor(x => x.BorrowerName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Borrower name is required.")
            .MaximumLength(200).WithMessage("Borrower name must not exceed 200 characters.");

        RuleFor(x => x.DueDate)
            .GreaterThan(x => x.IssueDate).WithMessage("Due date must be after issue date.");
    }
}

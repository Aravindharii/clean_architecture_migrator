using FluentValidation;
using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Validators;

public class LoanReturnRequestValidator : AbstractValidator<LoanReturnValidator>
{
    public LoanReturnRequestValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0).WithMessage("LoanId is required.");
    }
}

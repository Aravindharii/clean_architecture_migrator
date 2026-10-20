using FluentValidation;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class LoanReturnRequestValidator : AbstractValidator<LoanReturnValidator>
{
    public LoanReturnRequestValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0).WithMessage("LoanId is required.");
    }
}

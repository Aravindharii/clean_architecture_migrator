using FluentValidation;
using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

public class BookUpdateRequestValidator : AbstractValidator<BookUpdateValidator>
{
    public BookUpdateRequestValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Subtitle)
            .MaximumLength(500).WithMessage("Subtitle must not exceed 500 characters.");

        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("Author is required.");
    
        RuleFor(x => x.ISBN)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("ISBN is required.")
            .MaximumLength(20).WithMessage("ISBN must not exceed 20 characters.");

        RuleFor(x => x.ISBN13)
            .MaximumLength(20).WithMessage("ISBN13 must not exceed 20 characters.");

        RuleFor(x => x.Category)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.Language)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Language is required.")
            .MaximumLength(50).WithMessage("Language must not exceed 50 characters.");

        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1000, 2100).WithMessage("PublishedYear must be a valid year.");

        RuleFor(x => x.Edition)
            .MaximumLength(50).WithMessage("Edition must not exceed 50 characters.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            // .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.CoverImage)
            .MaximumLength(50000).WithMessage("CoverImage must not exceed 50000 characters.");
    }
}

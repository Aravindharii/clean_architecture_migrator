using FluentValidation;
using LibraryManagement.API.DTOs.Books;

namespace LibraryManagement.API.Validators;

/// <summary>
/// Validates data used when updating an existing book.
/// </summary>
public class BookUpdateDtoValidator : AbstractValidator<BookUpdateDto>
{
    public BookUpdateDtoValidator()
    {
        // Title
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title cannot contain only whitespace.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.");

        // Subtitle
        RuleFor(x => x.Subtitle)
            .MaximumLength(200)
            .WithMessage("Subtitle cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Subtitle));

        // Category
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("A valid category must be selected.");

        // Authors
        RuleFor(x => x.AuthorIds)
            .NotEmpty()
            .WithMessage("At least one author must be selected.");

        RuleForEach(x => x.AuthorIds)
            .GreaterThan(0)
            .WithMessage("Each author must have a valid identifier.");

        // Language
        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Language is required.")
            .Must(language => !string.IsNullOrWhiteSpace(language))
            .WithMessage("Language cannot contain only whitespace.")
            .MaximumLength(50)
            .WithMessage("Language cannot exceed 50 characters.");

        // Published Year
        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1450, DateTime.UtcNow.Year)
            .WithMessage(
                $"Published year must be between 1450 and {DateTime.UtcNow.Year}.");

        // Edition
        RuleFor(x => x.Edition)
            .MaximumLength(50)
            .WithMessage("Edition cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Edition));

        // Description
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .Must(description => !string.IsNullOrWhiteSpace(description))
            .WithMessage("Description cannot contain only whitespace.")
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.");

        // Cover Image
        RuleFor(x => x.CoverImage)
            .MaximumLength(500)
            .WithMessage("Cover image cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImage));
    }
}
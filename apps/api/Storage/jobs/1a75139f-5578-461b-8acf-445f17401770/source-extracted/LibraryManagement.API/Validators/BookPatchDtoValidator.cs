using FluentValidation;
using LibraryManagement.API.DTOs.Books;

namespace LibraryManagement.API.Validators;

/// <summary>
/// Validates data used when partially updating an existing book.
/// </summary>
public class BookPatchDtoValidator : AbstractValidator<BookPatchDto>
{
    public BookPatchDtoValidator()
    {
        // Title
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title cannot contain only whitespace.")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters.")
            .When(x => x.Title != null);

        // Subtitle
        RuleFor(x => x.Subtitle)
            .MaximumLength(200)
            .WithMessage("Subtitle cannot exceed 200 characters.")
            .When(x => x.Subtitle != null);

        // Category
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("A valid category must be selected.")
            .When(x => x.CategoryId.HasValue);

        // Authors
        RuleFor(x => x.AuthorIds)
            .NotEmpty()
            .WithMessage("At least one author must be selected.")
            .When(x => x.AuthorIds != null);

        RuleForEach(x => x.AuthorIds)
            .GreaterThan(0)
            .WithMessage("Each author must have a valid identifier.")
            .When(x => x.AuthorIds != null);

        // Language
        RuleFor(x => x.Language)
            .Must(language => !string.IsNullOrWhiteSpace(language))
            .WithMessage("Language cannot contain only whitespace.")
            .MaximumLength(50)
            .WithMessage("Language cannot exceed 50 characters.")
            .When(x => x.Language != null);

        // Published Year
        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1450, DateTime.UtcNow.Year)
            .WithMessage(
                $"Published year must be between 1450 and {DateTime.UtcNow.Year}.")
            .When(x => x.PublishedYear.HasValue);

        // Edition
        RuleFor(x => x.Edition)
            .MaximumLength(50)
            .WithMessage("Edition cannot exceed 50 characters.")
            .When(x => x.Edition != null);

        // Description
        RuleFor(x => x.Description)
            .Must(description => !string.IsNullOrWhiteSpace(description))
            .WithMessage("Description cannot contain only whitespace.")
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.")
            .When(x => x.Description != null);

        // Cover Image
        RuleFor(x => x.CoverImage)
            .MaximumLength(500)
            .WithMessage("Cover image cannot exceed 500 characters.")
            .When(x => x.CoverImage != null);
    }
}
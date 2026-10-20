using FluentValidation;
using Project.Application.Common.Dtos;

namespace Project.Application.Validators;

/// <summary>
/// Validates author update requests.
/// </summary>
public class AuthorUpdateValidator : AbstractValidator<AuthorUpdateDto>
{
    /// <summary>
    /// Initializes validation rules for updating an author.
    /// </summary>
    public AuthorUpdateValidator()
    {
        RuleFor(author => author.Name)
            .NotEmpty()
            .WithMessage("Author name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Author name cannot be empty or whitespace.")
            .Must(name => name.Trim().Length >= 2)
            .WithMessage("Author name must be at least 2 characters long.")
            .Must(name => name.Any(char.IsLetter))
            .WithMessage("Author name must contain at least one letter.")
            .MaximumLength(200)
            .WithMessage("Author name cannot exceed 200 characters.");

        RuleFor(author => author.Biography)
            .MaximumLength(2000)
            .WithMessage("Biography cannot exceed 2000 characters.");
    }
}
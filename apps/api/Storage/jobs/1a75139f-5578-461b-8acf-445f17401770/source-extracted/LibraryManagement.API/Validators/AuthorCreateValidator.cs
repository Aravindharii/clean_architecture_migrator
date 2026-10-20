using FluentValidation;
using LibraryManagement.API.DTOs.Authors;

namespace LibraryManagement.API.Validators;

/// <summary>
/// Validates author creation requests.
/// </summary>
public class AuthorCreateValidator : AbstractValidator<AuthorCreateDto>
{
    /// <summary>
    /// Initializes validation rules for creating an author.
    /// </summary>
    public AuthorCreateValidator()
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
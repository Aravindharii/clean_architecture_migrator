using Project.Application.Common.Dtos;
using Project.Application.Services;
using Project.Application.Authentication;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines business operations related to authors.
/// </summary>
public interface IAuthorService
{
    /// <summary>
    /// Retrieves authors with optional search.
    /// </summary>
    /// <param name="q">
    /// Optional search keyword used to search author names.
    /// </param>
    /// <returns>
    /// Collection of authors matching the search criteria.
    /// </returns>
    Task<IEnumerable<AuthorReadDto>> GetAllAsync(string? q);


    /// <summary>
    /// Retrieves an author by identifier.
    /// </summary>
    /// <param name="id">
    /// Author identifier.
    /// </param>
    /// <returns>
    /// Author details.
    /// </returns>
    Task<AuthorReadDto?> GetByIdAsync(int id);


    /// <summary>
    /// Retrieves books written by an author.
    /// </summary>
    /// <param name="authorId">
    /// Author identifier.
    /// </param>
    /// <returns>
    /// Collection of books written by the author.
    /// </returns>
    Task<IEnumerable<BookSummaryDto>> GetBooksByAuthorIdAsync(
        int authorId);

/// <summary>
/// Creates a new author.
/// </summary>
/// <param name="authorDto">
/// Data required to create the author.
/// </param>
/// <returns>
/// The created author.
/// </returns>
Task<AuthorReadDto> CreateAsync(
    AuthorCreateDto authorDto);

/// <summary>
/// Updates an existing author using optimistic concurrency.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <param name="authorDto">
/// Updated author data.
/// </param>
/// <param name="version">
/// Expected author version.
/// </param>
/// <returns>
/// The updated author.
/// </returns>
Task<AuthorReadDto> UpdateAsync(
    int id,
    AuthorUpdateDto authorDto,
    int version);

/// <summary>
/// Deletes an author if the author has no associated books.
/// </summary>
/// <param name="id">
/// Author identifier.
/// </param>
/// <returns>
/// True when the author is successfully deleted.
/// </returns>
Task<bool> DeleteAsync(int id);
}

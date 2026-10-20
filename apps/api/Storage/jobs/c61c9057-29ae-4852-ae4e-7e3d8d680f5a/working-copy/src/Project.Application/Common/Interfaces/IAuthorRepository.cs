using Project.Domain.Entities;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines data access operations for authors.
/// </summary>
public interface IAuthorRepository
{
    /// <summary>
    /// Retrieves authors with optional search.
    /// </summary>
    /// <param name="q">
    /// Optional search keyword for author name.
    /// </param>
    /// <returns>
    /// Collection of authors.
    /// </returns>
    Task<IEnumerable<Author>> GetAllAsync(string? q);


    /// <summary>
    /// Retrieves an author by identifier.
    /// </summary>
    /// <param name="id">
    /// Author identifier.
    /// </param>
    /// <returns>
    /// Author entity if found.
    /// </returns>
    Task<Author?> GetByIdAsync(int id);


    /// <summary>
    /// Retrieves books written by an author.
    /// </summary>
    /// <param name="authorId">
    /// Author identifier.
    /// </param>
    /// <returns>
    /// Collection of books linked to the author.
    /// </returns>
    Task<IEnumerable<Book>> GetBooksByAuthorIdAsync(int authorId);
    /// <summary>
/// Adds a new author.
/// </summary>
/// <param name="author">Author to add.</param>
Task AddAsync(Author author);

/// <summary>
/// Updates an existing author using optimistic concurrency.
/// </summary>
/// <param name="author">Author to update.</param>
/// <param name="expectedVersion">Expected author version.</param>
Task UpdateAsync(Author author, int expectedVersion);

/// <summary>
/// Deletes an author permanently.
/// </summary>
/// <param name="author">Author to delete.</param>
Task DeleteAsync(Author author);

/// <summary>
/// Checks whether an author is associated with any books.
/// </summary>
/// <param name="authorId">Author identifier.</param>
/// <returns>
/// True if the author is associated with one or more books.
/// </returns>
Task<bool> HasBooksAsync(int authorId);
}
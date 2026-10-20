using Project.Domain.Entities;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines data access operations for books.
/// </summary>
public interface IBookRepository
{
    /// <summary>
    /// Retrieves all books from the catalogue.
    /// </summary>
    Task<IEnumerable<Book>> GetAllAsync();


    /// <summary>
    /// Retrieves a book by its unique identifier.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    /// <returns>The matching book or null if not found.</returns>
    Task<Book?> GetByIdAsync(int id);

    /// <summary>
/// Returns a queryable collection of books for building catalogue queries.
/// </summary>
/// <returns>
/// An <see cref="IQueryable{Book}"/> that can be extended with
/// filtering, searching, sorting, and pagination.
/// </returns>
IQueryable<Book> Query();

    /// <summary>
/// Checks whether a book exists with the specified ISBN.
/// </summary>
/// <param name="isbn">ISBN to search for.</param>
/// <returns>True if a matching book exists; otherwise, false.</returns>
Task<bool> ExistsByIsbnAsync(string isbn);


/// <summary>
/// Checks whether a book exists with the specified ISBN13.
/// </summary>
/// <param name="isbn13">ISBN13 to search for.</param>
/// <returns>True if a matching book exists; otherwise, false.</returns>
Task<bool> ExistsByIsbn13Async(string isbn13);


    /// <summary>
    /// Adds a new book to the database.
    /// </summary>
    /// <param name="book">Book entity to add.</param>
    Task AddAsync(Book book);


   /// <summary>
/// Updates an existing book using optimistic concurrency.
/// </summary>
/// <param name="book">Updated book entity.</param>
/// <param name="expectedVersion">
/// Version expected by the client before the update.
/// </param>
Task UpdateAsync(Book book, int expectedVersion);


    /// <summary>
    /// Deletes a book from the database.
    /// </summary>
    /// <param name="book">Book entity to delete.</param>
    Task DeleteAsync(Book book);
}
using Project.Application.Common.Dtos;
using Project.Application.Services;
using Project.Application.Authentication;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines business operations related to books.
/// </summary>
public interface IBookService
{
/// <summary>
/// Retrieves books from the catalogue with optional
/// search, filtering, sorting, and pagination.
/// </summary>
/// <param name="q">
/// Optional search keyword used to search by title,
/// subtitle, or ISBN.
/// </param>
/// <param name="categoryId">
/// Optional category identifier used to filter books.
/// </param>
/// <param name="language">
/// Optional language filter.
/// </param>
/// <param name="publishedYear">
/// Optional published year filter.
/// </param>
/// <param name="available">
/// Optional availability filter.
/// When true, returns books with available copies greater than zero.
/// When false, returns books with no available copies.
/// </param>
/// <param name="page">
/// The page number to retrieve.
/// </param>
/// <param name="pageSize">
/// The number of books to return per page.
/// </param>
/// <param name="sortBy">
/// The field used for sorting.
/// </param>
/// <param name="sortOrder">
/// The sorting direction.
/// </param>
/// <returns>
/// A paginated collection of books matching the specified criteria.
/// </returns>
Task<PagedResult<BookReadDto>> GetAllAsync(
    string? q,
    int? categoryId,
    string? language,
    int? publishedYear,
    bool? available,
    int page,
    int pageSize,
    string? sortBy,
    string? sortOrder);

    /// <summary>
    /// Retrieves a book by its identifier.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    Task<BookReadDto?> GetByIdAsync(int id);


    /// <summary>
    /// Creates a new book.
    /// </summary>
    /// <param name="bookDto">Book creation data.</param>
    Task<BookReadDto> CreateAsync(BookCreateDto bookDto);


    /// <summary>
    /// Updates an existing book.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    /// <param name="bookDto">Updated book data.</param>
    Task<BookReadDto> UpdateAsync(
    int id,
    BookUpdateDto bookDto,
    int version);

    /// <summary>
    /// Deletes a book.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    Task<bool> DeleteAsync(int id);

    /// <summary>
/// Partially updates an existing book.
/// </summary>
/// <param name="id">Book identifier.</param>
/// <param name="bookDto">Fields to update.</param>
/// <param name="version">Expected book version.</param>
/// <returns>The updated book.</returns>
Task<BookReadDto> PatchAsync(
    int id,
    BookPatchDto bookDto,
    int version);
}
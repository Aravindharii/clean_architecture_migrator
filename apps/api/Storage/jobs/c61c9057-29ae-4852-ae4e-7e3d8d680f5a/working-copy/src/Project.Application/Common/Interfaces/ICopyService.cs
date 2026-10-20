using Project.Application.Common.Dtos;
using Project.Application.Services;
using Project.Application.Authentication;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines business operations for physical book copies.
/// </summary>
public interface ICopyService
{

    /// <summary>
/// Gets all active physical book copies.
/// </summary>
/// <returns>A collection of active copy DTOs.</returns>
Task<IEnumerable<CopyReadDto>> GetAllAsync();
    /// <summary>
    /// Gets all active copies belonging to a book.
    /// </summary>
    /// <param name="bookId">The identifier of the book.</param>
    /// <returns>A collection of copy DTOs.</returns>
    Task<IEnumerable<CopyReadDto>> GetByBookIdAsync(int bookId);

    /// <summary>
    /// Gets a specific copy by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the copy.</param>
    /// <returns>The requested copy.</returns>
    Task<CopyReadDto> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new physical book copy.
    /// </summary>
    /// <param name="bookId">The identifier of the associated book.</param>
    /// <param name="copyDto">The copy data.</param>
    /// <returns>The created copy.</returns>
    Task<CopyReadDto> CreateAsync(int bookId, CopyCreateDto copyDto);

    /// <summary>
    /// Updates an existing physical book copy.
    /// </summary>
    /// <param name="id">The identifier of the copy.</param>
    /// <param name="copyDto">The updated copy data.</param>
    /// <param name="version">The expected copy version.</param>
    Task UpdateAsync(int id, CopyUpdateDto copyDto, int version);

    /// <summary>
    /// Soft-deletes a physical book copy.
    /// </summary>
    /// <param name="id">The identifier of the copy.</param>
    /// <param name="version">The expected copy version.</param>
    Task DeleteAsync(int id, int version);
}
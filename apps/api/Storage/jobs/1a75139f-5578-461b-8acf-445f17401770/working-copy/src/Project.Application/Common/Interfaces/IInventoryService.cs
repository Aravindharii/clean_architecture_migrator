using Project.Application.Common.Dtos;
using Project.Application.Services;
using Project.Application.Authentication;

namespace Project.Application.Common.Interfaces;

/// <summary>
/// Defines business operations related to inventory.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Retrieves inventory information by book identifier.
    /// </summary>
    /// <param name="bookId">
    /// Book identifier.
    /// </param>
    /// <returns>
    /// Availability information of the book.
    /// </returns>
    Task<InventoryReadDto> GetByBookIdAsync(
        int bookId);


    /// <summary>
    /// Retrieves inventory information by ISBN.
    /// </summary>
    /// <param name="isbn">
    /// Book ISBN.
    /// </param>
    /// <returns>
    /// Availability information of the book.
    /// </returns>
    Task<InventoryReadDto> GetByIsbnAsync(
        string isbn);
}
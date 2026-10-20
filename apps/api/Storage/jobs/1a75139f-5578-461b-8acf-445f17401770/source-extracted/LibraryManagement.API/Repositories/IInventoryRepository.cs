using LibraryManagement.API.Models;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Defines data access operations for inventory.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Retrieves inventory information by book identifier.
    /// </summary>
    /// <param name="bookId">
    /// Book identifier.
    /// </param>
    /// <returns>
    /// Inventory record if found.
    /// </returns>
    Task<Inventory?> GetByBookIdAsync(int bookId);


    /// <summary>
    /// Retrieves inventory information by ISBN.
    /// </summary>
    /// <param name="isbn">
    /// Book ISBN.
    /// </param>
    /// <returns>
    /// Inventory record if found.
    /// </returns>
    Task<Inventory?> GetByIsbnAsync(string isbn);
}
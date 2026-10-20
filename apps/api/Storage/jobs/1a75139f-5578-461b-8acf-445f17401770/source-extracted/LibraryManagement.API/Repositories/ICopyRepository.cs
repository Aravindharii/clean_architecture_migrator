using LibraryManagement.API.Models;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Defines data-access operations for physical book copies.
/// </summary>
public interface ICopyRepository
{
    /// <summary>
    /// Gets all active physical book copies.
    /// </summary>
    Task<IEnumerable<Copy>> GetAllAsync();

    /// <summary>
    /// Gets all active copies belonging to a specific book.
    /// </summary>
    Task<IEnumerable<Copy>> GetByBookIdAsync(int bookId);

    /// <summary>
    /// Gets a specific active copy by its identifier.
    /// </summary>
    Task<Copy?> GetByIdAsync(int id);

    /// <summary>
    /// Checks whether a barcode already exists.
    /// </summary>
    Task<bool> ExistsByBarcodeAsync(string barcode);

    /// <summary>
    /// Gets copy counts for a specific book.
    /// </summary>
    Task<(int Total, int Available, int Loaned)>
        GetCountsByBookIdAsync(int bookId);

    /// <summary>
    /// Adds a new physical book copy.
    /// </summary>
    Task AddAsync(Copy copy);

    /// <summary>
    /// Adds multiple physical book copies.
    /// </summary>
    Task AddRangeAsync(IEnumerable<Copy> copies);

    /// <summary>
    /// Updates an existing physical book copy.
    /// </summary>
    Task UpdateAsync(Copy copy);

    /// <summary>
    /// Saves pending changes to the database.
    /// </summary>
    Task SaveChangesAsync();
}
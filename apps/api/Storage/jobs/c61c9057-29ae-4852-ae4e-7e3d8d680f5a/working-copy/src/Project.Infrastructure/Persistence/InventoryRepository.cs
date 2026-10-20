using Project.Infrastructure.Persistence;
using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Project.Application.Common.Interfaces;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Provides data access operations for inventory.
/// </summary>
public class InventoryRepository : IInventoryRepository
,InventoryRepository{
    private readonly AppDbContext _context;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="InventoryRepository"/> class.
    /// </summary>
    /// <param name="context">
    /// Database context.
    /// </param>
    public InventoryRepository(
        AppDbContext context)
    {
        _context = context;
    }


    /// <summary>
    /// Retrieves inventory by book identifier.
    /// </summary>
    /// <param name="bookId">
    /// Book identifier.
    /// </param>
    /// <returns>
    /// Inventory record if found.
    /// </returns>
    public async Task<Inventory?> GetByBookIdAsync(
        int bookId)
    {
        return await _context.Inventories
            .Include(inventory => inventory.Book)
            .FirstOrDefaultAsync(inventory =>
                inventory.BookId == bookId);
    }


    /// <summary>
    /// Retrieves inventory by ISBN.
    /// </summary>
    /// <param name="isbn">
    /// Book ISBN.
    /// </param>
    /// <returns>
    /// Inventory record if found.
    /// </returns>
    public async Task<Inventory?> GetByIsbnAsync(
        string isbn)
    {
        isbn = isbn.Trim();

        return await _context.Inventories
            .Include(inventory => inventory.Book)
            .FirstOrDefaultAsync(inventory =>
                inventory.Book.ISBN == isbn);
    }
}
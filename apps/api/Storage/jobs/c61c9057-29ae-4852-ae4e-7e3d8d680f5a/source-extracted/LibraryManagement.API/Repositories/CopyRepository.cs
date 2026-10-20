using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Provides data-access operations for physical book copies.
/// </summary>
public class CopyRepository : ICopyRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="CopyRepository"/> class.
    /// </summary>
    public CopyRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all active physical book copies.
    /// </summary>
    public async Task<IEnumerable<Copy>> GetAllAsync()
    {
        return await _context.Copies
            .Include(c => c.Book)
            .Where(c => !c.IsDeleted)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Copy>> GetByBookIdAsync(
        int bookId)
    {
        return await _context.Copies
            .Include(c => c.Book)
            .Where(c =>
                c.BookId == bookId &&
                !c.IsDeleted)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Copy?> GetByIdAsync(int id)
    {
        return await _context.Copies
            .Include(c => c.Book)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByBarcodeAsync(
        string barcode)
    {
        return await _context.Copies
            .AnyAsync(c =>
                c.Barcode == barcode &&
                !c.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<(int Total, int Available, int Loaned)>
        GetCountsByBookIdAsync(int bookId)
    {
        var copies = _context.Copies
            .Where(c =>
                c.BookId == bookId &&
                !c.IsDeleted);

        var total = await copies.CountAsync();

        var available = await copies
            .CountAsync(c => c.Status == "Available");

        var loaned = await copies
            .CountAsync(c => c.Status == "OnLoan");

        return (total, available, loaned);
    }

    /// <inheritdoc />
    public async Task AddAsync(Copy copy)
    {
        await _context.Copies.AddAsync(copy);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<Copy> copies)
    {
        await _context.Copies.AddRangeAsync(copies);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Copy copy)
    {
        _context.Copies.Update(copy);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
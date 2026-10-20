using Project.Infrastructure.Common;
using Project.Infrastructure.Persistence;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence;

public class CopyRepository : SoftDeleteRepository<Copy>, ICopyRepository
{
    public CopyRepository(AppDbContext context, ILogger<CopyRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Copy>> GetAllAsync()
    {
        return await _context.Copies
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .ToListAsync();
    }

    public async Task<Copy?> GetByIdAsync(int id)
    {
        return await _context.Copies
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public override async Task<IEnumerable<Copy>> GetAllDeletedAsync()
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .ToListAsync();
    }

    public override async Task<Copy?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task<Copy> AddAsync(Copy copy)
    {
        _context.Copies.Add(copy);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "Copy", _logger);
        }

        return copy;
    }

    public async Task UpdateAsync(Copy copy)
    {
        _context.Copies.Update(copy);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating Copy {Id}", copy.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "Copy", _logger);
        }
    }

    public async Task<bool> HasActiveLoansAsync(int copyId)
    {
        return await _context.Loans.AnyAsync(l => l.CopyId == copyId && l.Status == "Active");
    }

    public async Task<bool> ExistsByBookIdAsync(int bookId)
    {
        return await _context.Copies.AnyAsync(c => c.BookId == bookId);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Copies.CountAsync();
    }

    public async Task<int> GetAvailableCountAsync()
    {
        return await _context.Copies.CountAsync(c => c.Status == "Available");
    }

    public async Task<int> GetIssuedCountAsync()
    {
        return await _context.Copies.CountAsync(c => c.Status == "Issued");
    }

    public async Task<int> GetDamagedCountAsync()
    {
        return await _context.Copies.CountAsync(c => c.Status == "Damaged");
    }

    public async Task<IEnumerable<Copy>> QueryAsync(int? bookId = null, string? status = null)
    {
        var query = _context.Copies
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .AsQueryable();

        if (bookId.HasValue)
            query = query.Where(c => c.BookId == bookId.Value);

        if (status != null)
            query = query.Where(c => c.Status == status);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Copy>> GetAllByBookIdIncludingDeletedAsync(int bookId)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(c => c.BookId == bookId)
            .Include(c => c.Book)
            .Include(c => c.Loans)
            .ToListAsync();
    }

    public async Task<(int TotalCopies, int LoanedCopies)> GetDeleteCheckCountsAsync(int bookId)
    {
        var result = await _context.Copies
            .Where(c => c.BookId == bookId)
            .Select(c => new
            {
                c.Id,
                HasActiveLoan = c.Loans.Any(l => l.Status == "Active")
            })
            .ToListAsync();

        return (result.Count, result.Count(c => c.HasActiveLoan));
    }
}

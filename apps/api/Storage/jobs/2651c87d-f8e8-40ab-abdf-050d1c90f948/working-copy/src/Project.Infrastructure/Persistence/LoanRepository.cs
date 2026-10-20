using Project.Infrastructure.Common;
using Project.Infrastructure.Persistence;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoanRepository> _logger;

    public LoanRepository(AppDbContext context, ILogger<LoanRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Loan>> GetAllAsync()
    {
        return await _context.Loans
            .Include(l => l.Copy)
                .ThenInclude(c => c!.Book)
            .ToListAsync();
    }

    public async Task<IEnumerable<Loan>> GetByUserIdAsync(int userId)
    {
        return await _context.Loans
            .Include(l => l.Copy)
                .ThenInclude(c => c!.Book)
            .Where(l => l.UserId == userId)
            .ToListAsync();
    }

    public async Task<Loan?> GetByIdAsync(int id)
    {
        return await _context.Loans
            .Include(l => l.Copy)
                .ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Loan> AddAsync(Loan loan)
    {
        _context.Loans.Add(loan);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "Loan", _logger);
        }

        return loan;
    }

    public async Task UpdateAsync(Loan loan)
    {
        _context.Loans.Update(loan);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating Loan {Id}", loan.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "Loan", _logger);
        }
    }

    public Task DeleteAsync(Loan loan)
    {
        throw new InvalidOperationException("Loan records are audit data and cannot be deleted.");
    }

    public async Task<bool> HasActiveLoanForCopyAsync(int copyId)
    {
        return await _context.Loans.AnyAsync(l => l.CopyId == copyId && l.Status == "Active");
    }

    public async Task<int> GetActiveLoanCountAsync()
    {
        return await _context.Loans.CountAsync(l => l.Status == "Active");
    }

    public async Task<int> GetOverdueLoanCountAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Loans.CountAsync(l => l.Status == "Active" && l.DueDate < now);
    }
}

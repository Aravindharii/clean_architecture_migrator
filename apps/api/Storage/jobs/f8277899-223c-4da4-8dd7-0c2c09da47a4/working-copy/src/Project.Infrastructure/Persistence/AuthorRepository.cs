using Project.Infrastructure.Common;
using Project.Infrastructure.Persistence;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence;

public class AuthorRepository : SoftDeleteRepository<Author>, IAuthorRepository
{
    public AuthorRepository(AppDbContext context, ILogger<AuthorRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Author>> GetAllAsync()
    {
        return await _context.Authors
            .Include(a => a.Books)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        return await _context.Authors
            .Include(a => a.Books)
                .ThenInclude(b => b.Copies)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public override async Task<IEnumerable<Author>> GetAllDeletedAsync()
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .Include(a => a.Books)
            .ToListAsync();
    }

    public override async Task<Author?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(a => a.Books)
                .ThenInclude(b => b.Copies)
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task<Author> AddAsync(Author author)
    {
        _context.Authors.Add(author);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "Author", _logger);
        }

        return author;
    }

    public async Task UpdateAsync(Author author)
    {
        _context.Authors.Update(author);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating Author {Id}", author.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "Author", _logger);
        }
    }

    public async Task<bool> HasBooksAsync(int authorId)
    {
        return await _context.Books.AnyAsync(b => b.AuthorId == authorId);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Authors.CountAsync();
    }
}

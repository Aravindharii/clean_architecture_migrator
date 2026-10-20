using LibraryManagement.Api.Common;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class BookRepository : SoftDeleteRepository<Book>, IBookRepository
{
    public BookRepository(AppDbContext context, ILogger<BookRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _context.Books.Include(b => b.Author).Include(b => b.Copies).ToListAsync();
    }

    public async Task<(IEnumerable<Book> Books, int TotalCount)> GetAllAsync(string? q, string? category, bool? available, int page, int size, string? sort)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(term) ||
                (b.Subtitle != null && b.Subtitle.ToLower().Contains(term)) ||
                b.Author.Name.ToLower().Contains(term) ||
                b.ISBN.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(b => b.Category == category);
        }

        if (available.HasValue && available.Value)
        {
            query = query.Where(b => b.Copies.Any(c => c.Status == "Available"));
        }

        var totalCount = await query.CountAsync();

        IQueryable<Book> dataQuery = query
            .Include(b => b.Author)
            .Include(b => b.Copies);

        dataQuery = sort?.ToLower() switch
        {
            "title" => dataQuery.OrderBy(b => b.Title),
            "author" => dataQuery.OrderBy(b => b.Author.Name),
            "publishedyear" => dataQuery.OrderBy(b => b.PublishedYear),
            "category" => dataQuery.OrderBy(b => b.Category),
            _ => dataQuery.OrderBy(b => b.Title)
        };

        var books = await dataQuery
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (books, totalCount);
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books.Include(b => b.Author).Include(b => b.Copies).FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<IEnumerable<Book>> GetAllDeletedAsync()
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted)
            .Include(b => b.Author)
            .Include(b => b.Copies)
            .ToListAsync();
    }

    public override async Task<Book?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(b => b.Author)
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task<Book> AddAsync(Book book)
    {
        _context.Books.Add(book);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "Book", _logger);
        }

        return book;
    }

    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating Book {Id}", book.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "Book", _logger);
        }
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Books.CountAsync();
    }
}

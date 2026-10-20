using LibraryManagement.API.Data;
using LibraryManagement.API.Exceptions;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.API.Repositories;

/// <summary>
/// Provides database operations for the Book entity.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public IQueryable<Book> Query()
    {
        return _context.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author);
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn)
    {
        return await _context.Books
            .AnyAsync(book => book.ISBN == isbn);
    }

    public async Task<bool> ExistsByIsbn13Async(string isbn13)
    {
        return await _context.Books
            .AnyAsync(book => book.ISBN13 == isbn13);
    }

    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
         await _context.SaveChangesAsync();
       
    }

    public async Task UpdateAsync(Book book, int expectedVersion)
    {
        _context.Books.Attach(book);

        _context.Entry(book)
            .Property(b => b.Version)
            .OriginalValue = expectedVersion;

        _context.Entry(book).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "The book was modified by another user. Please refresh and try again.");
        }
    }

    public async Task DeleteAsync(Book book)
    {
        _context.Books.Attach(book);

        book.IsDeleted = true;

        _context.Entry(book)
            .Property(b => b.IsDeleted)
            .IsModified = true;

        await _context.SaveChangesAsync();
    }
}
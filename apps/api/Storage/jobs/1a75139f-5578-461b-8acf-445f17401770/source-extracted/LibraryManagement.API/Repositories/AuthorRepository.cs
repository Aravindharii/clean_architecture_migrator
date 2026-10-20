using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using LibraryManagement.API.Exceptions;
namespace LibraryManagement.API.Repositories;

/// <summary>
/// Provides database operations for authors.
/// </summary>
public class AuthorRepository : IAuthorRepository
{
    private readonly AppDbContext _context;


    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="AuthorRepository"/> class.
    /// </summary>
    /// <param name="context">
    /// Library database context.
    /// </param>
    public AuthorRepository(AppDbContext context)
    {
        _context = context;
    }


   /// <summary>
/// Retrieves authors with optional search.
/// </summary>
/// <param name="q">
/// Optional search keyword for author name.
/// </param>
/// <returns>
/// Collection of authors.
/// </returns>
public async Task<IEnumerable<Author>> GetAllAsync(string? q)
{
    var query = _context.Authors
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
    {
        q = q.Trim();

        query = query.Where(author =>
            EF.Functions.ILike(
                author.Name,
                $"%{q}%"));
    }

    return await query.ToListAsync();
}


    /// <summary>
    /// Retrieves an author by identifier.
    /// </summary>
    public async Task<Author?> GetByIdAsync(int id)
    {
        return await _context.Authors
            .Include(author => author.BookAuthors)
                .ThenInclude(bookAuthor => bookAuthor.Book)
            .FirstOrDefaultAsync(author => author.Id == id);
    }

    /// <summary>
/// Retrieves books written by an author.
/// </summary>
/// <param name="authorId">
/// Author identifier.
/// </param>
/// <returns>
/// Collection of books.
/// </returns>
public async Task<IEnumerable<Book>> GetBooksByAuthorIdAsync(
    int authorId)
{
    return await _context.BookAuthors
        .Where(bookAuthor =>
            bookAuthor.AuthorId == authorId)
        .Select(bookAuthor =>
            bookAuthor.Book)
        .ToListAsync();
}

/// <summary>
/// Adds a new author to the database.
/// </summary>
/// <param name="author">The author to add.</param>
public async Task AddAsync(Author author)
{
    await _context.Authors.AddAsync(author);
    await _context.SaveChangesAsync();
}

/// <summary>
/// Updates an existing author using optimistic concurrency.
/// </summary>
/// <param name="author">The author to update.</param>
/// <param name="expectedVersion">The version expected by the client.</param>
public async Task UpdateAsync(
    Author author,
    int expectedVersion)
{
    _context.Authors.Attach(author);

    _context.Entry(author)
        .Property(a => a.Version)
        .OriginalValue = expectedVersion;

    _context.Entry(author).State =
        EntityState.Modified;

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new ConcurrencyException(
            "The author was modified by another user. Please refresh and try again.");
    }
}

//// <summary>
/// Permanently deletes an author after removing relationships
/// with soft-deleted books.
/// </summary>
/// <param name="author">The author to delete.</param>
public async Task DeleteAsync(Author author)
{
    var oldRelationships = await _context.BookAuthors
        .IgnoreQueryFilters()
        .Where(ba =>
            ba.AuthorId == author.Id &&
            ba.Book.IsDeleted)
        .ToListAsync();

    _context.BookAuthors.RemoveRange(oldRelationships);

    _context.Authors.Remove(author);

    await _context.SaveChangesAsync();
}
/// <summary>
/// Checks whether an author is associated with any books.
/// </summary>
/// <param name="authorId">Author identifier.</param>
/// <returns>
/// True if the author has one or more associated books.
/// </returns>
public async Task<bool> HasBooksAsync(int authorId)
{
    return await _context.BookAuthors
        .AnyAsync(ba =>
            ba.AuthorId == authorId &&
            !ba.Book.IsDeleted);
}}

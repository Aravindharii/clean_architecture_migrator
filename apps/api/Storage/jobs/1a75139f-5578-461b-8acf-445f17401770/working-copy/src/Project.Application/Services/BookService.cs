using Project.Application.Common.Dtos;
using Project.Application.Common.Exceptions;
using Project.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Project.Application.Common.Interfaces;
using Project.Application.Authentication;

namespace Project.Application.Services;

/// <summary>
/// Provides business operations for books.
/// </summary>
public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly ICopyRepository _copyRepository;

    public BookService(
        IBookRepository bookRepository,
        ICopyRepository copyRepository)
    {
        _bookRepository = bookRepository;
        _copyRepository = copyRepository;
    }

    public async Task<PagedResult<BookReadDto>> GetAllAsync(
        string? q,
        int? categoryId,
        string? language,
        int? publishedYear,
        bool? available,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder)
    {
        var query = _bookRepository.Query();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{q}%") ||
                EF.Functions.ILike(book.ISBN, $"%{q}%") ||
                book.BookAuthors.Any(ba =>
                    EF.Functions.ILike(
                        ba.Author.Name,
                        $"%{q}%")));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(book =>
                book.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            language = language.Trim();

            query = query.Where(book =>
                EF.Functions.ILike(
                    book.Language,
                    language));
        }

        if (publishedYear.HasValue)
        {
            query = query.Where(book =>
                book.PublishedYear == publishedYear.Value);
        }

        if (available.HasValue)
        {
            query = available.Value
                ? query.Where(book =>
                    book.Copies.Any(c =>
                        c.Status == "Available"))
                : query.Where(book =>
                    !book.Copies.Any(c =>
                        c.Status == "Available"));
        }

        var isDescending = string.Equals(
            sortOrder,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        query = sortBy?.Trim().ToLower() switch
        {
            "title" => isDescending
                ? query.OrderByDescending(b => b.Title)
                    .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Title)
                    .ThenBy(b => b.Id),

            "publishedyear" => isDescending
                ? query.OrderByDescending(
                        b => b.PublishedYear)
                    .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.PublishedYear)
                    .ThenBy(b => b.Id),

            "available" => isDescending
                ? query.OrderByDescending(b =>
                    b.Copies.Count(c =>
                        c.Status == "Available"))
                    .ThenByDescending(b => b.Id)
                : query.OrderBy(b =>
                    b.Copies.Count(c =>
                        c.Status == "Available"))
                    .ThenBy(b => b.Id),

            _ => query.OrderBy(b => b.Title)
                .ThenBy(b => b.Id)
        };
        
        var totalItems = await query.CountAsync();

        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        if (page > totalPages && totalPages > 0)
        {
            throw new NotFoundException(
                "Requested page does not exist.");
        }

        return new PagedResult<BookReadDto>
        {
            Items = books.Select(MapToReadDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<BookReadDto?> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);

        if (book == null)
        {
            throw new NotFoundException(
                $"Book with ID '{id}' was not found.");
        }

        return MapToReadDto(book);
    }

    public async Task<BookReadDto> CreateAsync(
        BookCreateDto bookDto)
    {
        if (await _bookRepository
            .ExistsByIsbnAsync(bookDto.ISBN))
        {
            throw new DuplicateResourceException(
                $"A book with ISBN '{bookDto.ISBN}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(bookDto.ISBN13) &&
            await _bookRepository
                .ExistsByIsbn13Async(bookDto.ISBN13))
        {
            throw new DuplicateResourceException(
                $"A book with ISBN13 '{bookDto.ISBN13}' already exists.");
        }

        var book = new Book
        {
            Title = bookDto.Title,
            Subtitle = bookDto.Subtitle,
            ISBN = bookDto.ISBN,
            ISBN13 = bookDto.ISBN13,
            CategoryId = bookDto.CategoryId,
            Language = bookDto.Language,
            PublishedYear = bookDto.PublishedYear,
            Edition = bookDto.Edition,
            Description = bookDto.Description,
            CoverImage = bookDto.CoverImage
        };

        book.BookAuthors = bookDto.AuthorIds
            .Select(authorId => new BookAuthor
            {
                AuthorId = authorId
            })
            .ToList();

        await _bookRepository.AddAsync(book);

        var copies = Enumerable.Range(
                1,
                bookDto.TotalCopies)
            .Select(i => new Copy
            {
                BookId = book.Id,
                Barcode = $"LIB-{book.Id:D6}-{i:D3}",
                Location = "Unassigned",
                Status = "Available",
                Version = 1,
                IsDeleted = false
            })
            .ToList();

        await _copyRepository.AddRangeAsync(copies);
        await _copyRepository.SaveChangesAsync();

        var createdBook =
            await _bookRepository.GetByIdAsync(book.Id);

        return MapToReadDto(createdBook!);
    }

    public async Task<BookReadDto> UpdateAsync(
    int id,
    BookUpdateDto bookDto,
    int version)
{
    var book = await _bookRepository.GetByIdAsync(id);

    if (book == null)
    {
        throw new NotFoundException(
            $"Book with ID '{id}' was not found.");
    }

    if (book.Version != version)
    {
        throw new ConcurrencyException(
            "The book was modified by another user. Please refresh and try again.");
    }

    book.Title = bookDto.Title;
    book.Subtitle = bookDto.Subtitle;
    book.CategoryId = bookDto.CategoryId;
    book.Language = bookDto.Language;
    book.PublishedYear = bookDto.PublishedYear;
    book.Edition = bookDto.Edition;
    book.Description = bookDto.Description;
    book.CoverImage = bookDto.CoverImage;
    book.Version++;

    await _bookRepository.UpdateAsync(
        book,
        version);

    var updatedBook =
        await _bookRepository.GetByIdAsync(id);

    return MapToReadDto(updatedBook!);
}   
 public async Task<BookReadDto> PatchAsync(
    int id,
    BookPatchDto bookDto,
    int version)
{
    var book = await _bookRepository.GetByIdAsync(id);

    if (book == null)
    {
        throw new NotFoundException(
            $"Book with ID '{id}' was not found.");
    }

    if (book.Version != version)
    {
        throw new ConcurrencyException(
            "The book was modified by another user. Please refresh and try again.");
    }

    if (bookDto.Title != null)
        book.Title = bookDto.Title;

    if (bookDto.Subtitle != null)
        book.Subtitle = bookDto.Subtitle;

    if (bookDto.CategoryId.HasValue)
        book.CategoryId = bookDto.CategoryId.Value;

    if (bookDto.AuthorIds != null)
    {
        book.BookAuthors = bookDto.AuthorIds
            .Select(authorId => new BookAuthor
            {
                AuthorId = authorId
            })
            .ToList();
    }

    if (bookDto.Language != null)
        book.Language = bookDto.Language;

    if (bookDto.PublishedYear.HasValue)
        book.PublishedYear =
            bookDto.PublishedYear.Value;

    if (bookDto.Edition != null)
        book.Edition = bookDto.Edition;

    if (bookDto.Description != null)
        book.Description = bookDto.Description;

    if (bookDto.CoverImage != null)
        book.CoverImage = bookDto.CoverImage;

    book.Version++;

    await _bookRepository.UpdateAsync(
        book,
        version);

    var updatedBook =
        await _bookRepository.GetByIdAsync(id);

    return MapToReadDto(updatedBook!);
}

public async Task<bool> DeleteAsync(int id)
{
    var book = await _bookRepository.GetByIdAsync(id);

    if (book == null)
    {
        throw new NotFoundException(
            $"Book with ID '{id}' was not found.");
    }

    if (book.Copies.Any())
    {
        throw new ConflictException(
            "The book cannot be deleted because physical copies exist.");
    }

    await _bookRepository.DeleteAsync(book);

    return true;
}

    private static BookReadDto MapToReadDto(Book book)
    {
        var totalCopies = book.Copies.Count;

        var availableCopies = book.Copies.Count(
            c => c.Status == "Available");

        var loanedCopies = book.Copies.Count(
            c => c.Status == "OnLoan");

        return new BookReadDto
        {
            Id = book.Id,
            Version = book.Version,
            Title = book.Title,
            Subtitle = book.Subtitle,

            Authors = book.BookAuthors
                .Select(ba => new AuthorSummaryDto
                {
                    Id = ba.Author.Id,
                    Name = ba.Author.Name
                })
                .ToList(),

            ISBN = book.ISBN,
            ISBN13 = book.ISBN13,
            CategoryId = book.CategoryId,
            Category = book.Category.Name,
            Language = book.Language,
            PublishedYear = book.PublishedYear,
            Edition = book.Edition,
            Description = book.Description,
            CoverImage = book.CoverImage,

            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            LoanedCopies = loanedCopies
        };
    }
}
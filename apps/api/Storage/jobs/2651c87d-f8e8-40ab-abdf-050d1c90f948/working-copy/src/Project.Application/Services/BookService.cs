using System.Security.Claims;
using Project.Application.Common.ETag;
using Project.Application.Common.Exceptions;
using Project.Application.Common.Webhooks;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuthorRepository _authorRepository;
    private readonly ICopyRepository _copyRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebhookEventPublisher _webhookEventPublisher;
    private readonly IETagService _etagService;
    private readonly ILogger<BookService> _logger;

    public BookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        ICopyRepository copyRepository,
        IHttpContextAccessor httpContextAccessor,
        IWebhookEventPublisher webhookEventPublisher,
        IETagService etagService,
        ILogger<BookService> logger)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _copyRepository = copyRepository;
        _httpContextAccessor = httpContextAccessor;
        _webhookEventPublisher = webhookEventPublisher;
        _etagService = etagService;
        _logger = logger;
    }

    public async Task<PagedResponse<BookDto>> GetAllAsync(string? q, string? category, bool? available, int page, int size, string? sort)
    {
        var (books, totalItems) = await _bookRepository.GetAllAsync(q, category, available, page, size, sort);
        return new PagedResponse<BookDto>
        {
            Items = books.Select(MapToDto).ToList(),
            Page = page,
            Size = size,
            TotalItems = totalItems
        };
    }

    public async Task<IEnumerable<BookDto>> GetAllAsync()
    {
        var books = await _bookRepository.GetAllAsync();
        return books.Select(MapToDto);
    }

    public async Task<BookDto> GetByIdAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");
        return MapToDto(book);
    }

    public async Task<BookDto> CreateAsync(BookCreateValidator dto)
    {
        var author = await _authorRepository.GetByIdAsync(dto.AuthorId)
            ?? throw new NotFoundException($"Author with ID {dto.AuthorId} not found.");
        var book = new Book
        {
            Title = dto.Title,
            Subtitle = dto.Subtitle,
            AuthorId = dto.AuthorId,
            ISBN = dto.ISBN,
            ISBN13 = dto.ISBN13,
            Category = dto.Category,
            Language = dto.Language,
            PublishedYear = dto.PublishedYear,
            Edition = dto.Edition,
            Description = dto.Description,
            CoverImage = dto.CoverImage
        };
        await _bookRepository.AddAsync(book);
        if (dto.TotalCopies > 0)
        {
            var copies = new List<Copy>();
            for (int i = 1; i <= dto.TotalCopies; i++)
            {
                copies.Add(new Copy
                {
                    BookId = book.Id,
                    Barcode = $"BC-{book.Id}-{i:D3}",
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow
                });
            }
            foreach (var copy in copies)
            {
                await _copyRepository.AddAsync(copy);
            }
        }
        var result = await _bookRepository.GetByIdAsync(book.Id);

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.BookCreated,
            new { bookId = book.Id, title = book.Title, authorId = book.AuthorId });

        return MapToDto(result!);
    }

    public async Task UpdateAsync(int id, BookUpdateValidator dto)
    {
        var book = await _bookRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), book.Id, book.UpdatedAt);
        var author = await _authorRepository.GetByIdAsync(dto.AuthorId)
            ?? throw new NotFoundException($"Author with ID {dto.AuthorId} not found.");
        book.Title = dto.Title;
        book.Subtitle = dto.Subtitle;
        book.AuthorId = dto.AuthorId;
        book.ISBN = dto.ISBN;
        book.ISBN13 = dto.ISBN13;
        book.Category = dto.Category;
        book.Language = dto.Language;
        book.PublishedYear = dto.PublishedYear;
        book.Edition = dto.Edition;
        book.Description = dto.Description;
        book.CoverImage = dto.CoverImage;
        await _bookRepository.UpdateAsync(book);

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.BookUpdated,
            new { bookId = book.Id, title = book.Title, authorId = book.AuthorId });
    }

    public async Task DeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");
        if (book.IsDeleted)
            return;
        var (totalCopies, loanedCopies) = await _copyRepository.GetDeleteCheckCountsAsync(id);
        if (loanedCopies > 0)
            throw new BookCannotBeDeletedException(id, totalCopies, totalCopies - loanedCopies, loanedCopies, "ACTIVE_LOANS");
        await _bookRepository.SoftDeleteAsync(book, GetCurrentUserEmail());

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.BookDeleted,
            new { bookId = book.Id, title = book.Title });
    }

    public async Task<BookDeleteCheckResult> CheckDeletionEligibilityAsync(int id)
    {
        var book = await _bookRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");

        if (book.IsDeleted)
        {
            return new BookDeleteCheckResult
            {
                CanDelete = false,
                TotalCopies = 0,
                AvailableCopies = 0,
                LoanedCopies = 0,
                Reason = "ALREADY_DELETED"
            };
        }

        var (totalCopies, loanedCopies) = await _copyRepository.GetDeleteCheckCountsAsync(id);
        var availableCopies = totalCopies - loanedCopies;

        string? reason = null;
        if (totalCopies > 0 && loanedCopies > 0)
            reason = "ACTIVE_LOANS";
        else if (totalCopies > 0)
            reason = "COPIES_EXIST";

        return new BookDeleteCheckResult
        {
            CanDelete = loanedCopies == 0,
            TotalCopies = totalCopies,
            AvailableCopies = availableCopies,
            LoanedCopies = loanedCopies,
            Reason = reason
        };
    }

    public async Task<IEnumerable<BookDto>> GetDeletedAsync()
    {
        var books = await _bookRepository.GetAllDeletedAsync();
        return books.Select(MapToDto);
    }

    public async Task RestoreAsync(int id)
    {
        var book = await _bookRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");
        if (!book.IsDeleted)
            throw new BadRequestException($"Book with ID {id} is not deleted.");
        await _bookRepository.RestoreAsync(book);
    }

    public async Task HardDeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Book with ID {id} not found.");
        var copies = await _copyRepository.GetAllByBookIdIncludingDeletedAsync(id);
        foreach (var copy in copies)
            await _copyRepository.HardDeleteAsync(copy);
        await _bookRepository.HardDeleteAsync(book);
    }

    private string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;
    }

    private string? GetIfMatchHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.IfMatch.ToString();
    }

    private static BookDto MapToDto(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Subtitle = book.Subtitle,
        AuthorId = book.AuthorId,
        Author = book.Author?.Name ?? string.Empty,
        ISBN = book.ISBN,
        ISBN13 = book.ISBN13,
        Category = book.Category,
        Language = book.Language,
        PublishedYear = book.PublishedYear,
        Edition = book.Edition,
        Description = book.Description,
        CoverImage = book.CoverImage,
        IsDeleted = book.IsDeleted,
        UpdatedAt = book.UpdatedAt,
        DeletedAt = book.DeletedAt,
        DeletedBy = book.DeletedBy,
        TotalCopies = book.Copies?.Count ?? 0,
        AvailableCopies = book.Copies?.Count(c => c.Status == "Available") ?? 0,
        Copies = book.Copies?.Select(c => new CopyDto
        {
            Id = c.Id,
            BookId = c.BookId,
            BookTitle = book.Title,
            Barcode = c.Barcode,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        }).ToList() ?? new()
    };
}

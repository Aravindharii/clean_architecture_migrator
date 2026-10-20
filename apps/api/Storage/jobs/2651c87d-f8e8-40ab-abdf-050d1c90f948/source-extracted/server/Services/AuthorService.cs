using System.Security.Claims;
using LibraryManagement.Api.Common.ETag;
using LibraryManagement.Api.Common.Exceptions;
using LibraryManagement.Api.DTOs;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories.Interfaces;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IETagService _etagService;
    private readonly ILogger<AuthorService> _logger;

    public AuthorService(IAuthorRepository authorRepository, IHttpContextAccessor httpContextAccessor, IETagService etagService, ILogger<AuthorService> logger)
    {
        _authorRepository = authorRepository;
        _httpContextAccessor = httpContextAccessor;
        _etagService = etagService;
        _logger = logger;
    }

    public async Task<IEnumerable<AuthorDto>> GetAllAsync()
    {
        var authors = await _authorRepository.GetAllAsync();
        return authors.Select(author => new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            UpdatedAt = author.UpdatedAt,
            BookCount = author.Books?.Count ?? 0
        });
    }

    public async Task<AuthorDto> GetByIdAsync(int id)
    {
        var author = await _authorRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author with ID {id} not found.");
        return new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            UpdatedAt = author.UpdatedAt,
            BookCount = author.Books?.Count ?? 0,
            Books = author.Books?.Select(book => new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Subtitle = book.Subtitle,
                AuthorId = book.AuthorId,
                Author = author.Name,
                ISBN = book.ISBN,
                ISBN13 = book.ISBN13,
                Category = book.Category,
                Language = book.Language,
                PublishedYear = book.PublishedYear,
                Edition = book.Edition,
                Description = book.Description,
                CoverImage = book.CoverImage,
                UpdatedAt = book.UpdatedAt,
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
            }).ToList() ?? new()
        };
    }

    public async Task<AuthorDto> CreateAsync(AuthorCreateValidator dto)
    {
        var author = new Author
        {
            Name = dto.Name
        };
        await _authorRepository.AddAsync(author);
        return new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            UpdatedAt = author.UpdatedAt,
            BookCount = author.Books?.Count ?? 0
        };
    }

    public async Task UpdateAsync(int id, AuthorUpdateValidator dto)
    {
        var author = await _authorRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Author with ID {id} not found.");
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), author.Id, author.UpdatedAt);
        author.Name = dto.Name;
        await _authorRepository.UpdateAsync(author);
    }

    public async Task DeleteAsync(int id)
    {
        var author = await _authorRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Author with ID {id} not found.");
        if (author.IsDeleted)
            return;
        var hasBooks = await _authorRepository.HasBooksAsync(id);
        if (hasBooks)
            throw new BadRequestException($"Author with ID {id} cannot be deleted because they have associated books.");
        await _authorRepository.SoftDeleteAsync(author, GetCurrentUserEmail());
    }

    public async Task<IEnumerable<AuthorDto>> GetDeletedAsync()
    {
        var authors = await _authorRepository.GetAllDeletedAsync();
        return authors.Select(author => new AuthorDto
        {
            Id = author.Id,
            Name = author.Name,
            UpdatedAt = author.UpdatedAt,
            BookCount = author.Books?.Count ?? 0
        });
    }

    public async Task RestoreAsync(int id)
    {
        var author = await _authorRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Author with ID {id} not found.");
        if (!author.IsDeleted)
            throw new BadRequestException($"Author with ID {id} is not deleted.");
        await _authorRepository.RestoreAsync(author);
    }

    public async Task HardDeleteAsync(int id)
    {
        var author = await _authorRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Author with ID {id} not found.");
        var hasBooks = await _authorRepository.HasBooksAsync(id);
        if (hasBooks)
            throw new BadRequestException($"Author with ID {id} cannot be permanently deleted because they have associated books.");
        await _authorRepository.HardDeleteAsync(author);
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
}

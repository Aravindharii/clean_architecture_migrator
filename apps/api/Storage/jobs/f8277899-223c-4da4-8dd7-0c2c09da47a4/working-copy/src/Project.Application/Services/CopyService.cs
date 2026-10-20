using System.Security.Claims;
using Project.Application.Common.ETag;
using Project.Application.Common.Exceptions;
using Project.Application.Common.Webhooks;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Services;

public class CopyService : ICopyService
{
    private readonly ICopyRepository _copyRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebhookEventPublisher _webhookEventPublisher;
    private readonly IETagService _etagService;
    private readonly ILogger<CopyService> _logger;

    public CopyService(ICopyRepository copyRepository, IBookRepository bookRepository, IHttpContextAccessor httpContextAccessor, IWebhookEventPublisher webhookEventPublisher, IETagService etagService, ILogger<CopyService> logger)
    {
        _copyRepository = copyRepository;
        _bookRepository = bookRepository;
        _httpContextAccessor = httpContextAccessor;
        _webhookEventPublisher = webhookEventPublisher;
        _etagService = etagService;
        _logger = logger;
    }

    public async Task<IEnumerable<CopyDto>> GetAllAsync()
    {
        var copies = await _copyRepository.GetAllAsync();
        return copies.Select(MapToDto);
    }

    public async Task<CopyDto> GetByIdAsync(int id)
    {
        var copy = await _copyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Copy with ID {id} not found.");
        return MapToDto(copy);
    }

    public async Task<CopyDto> CreateAsync(CopyCreateValidator dto)
    {
        var book = await _bookRepository.GetByIdAsync(dto.BookId)
            ?? throw new NotFoundException($"Book with ID {dto.BookId} not found.");
        var copy = new Copy
        {
            BookId = dto.BookId,
            Barcode = dto.Barcode,
            Status = "Available",
            CreatedAt = DateTime.UtcNow
        };
        await _copyRepository.AddAsync(copy);
        return new CopyDto
        {
            Id = copy.Id,
            BookId = copy.BookId,
            BookTitle = book.Title,
            Barcode = copy.Barcode,
            Status = copy.Status,
            CreatedAt = copy.CreatedAt,
            UpdatedAt = copy.UpdatedAt
        };
    }

    public async Task UpdateAsync(int id, CopyUpdateValidator dto)
    {
        var copy = await _copyRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Copy with ID {id} not found.");
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), copy.Id, copy.UpdatedAt);
        var previousStatus = copy.Status;
        copy.Barcode = dto.Barcode;
        copy.Status = dto.Status;
        await _copyRepository.UpdateAsync(copy);

        if (!string.Equals(previousStatus, copy.Status, StringComparison.Ordinal))
        {
            _webhookEventPublisher.TryPublish(
                WebhookEventTypes.CopyStatusChanged,
                new { copyId = copy.Id, bookId = copy.BookId, barcode = copy.Barcode, status = copy.Status, previousStatus });
        }
    }

    public async Task DeleteAsync(int id)
    {
        var copy = await _copyRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Copy with ID {id} not found.");
        if (copy.IsDeleted)
            return;
        var hasActiveLoans = await _copyRepository.HasActiveLoansAsync(id);
        if (hasActiveLoans)
            throw new BadRequestException($"Copy with ID {id} cannot be deleted because it has active loans.");
        await _copyRepository.SoftDeleteAsync(copy, GetCurrentUserEmail());
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

    public async Task<IEnumerable<CopyDto>> QueryAsync(int? bookId = null, string? status = null)
    {
        var copies = await _copyRepository.QueryAsync(bookId, status);
        return copies.Select(MapToDto);
    }

    public async Task<IEnumerable<CopyDto>> GetDeletedAsync()
    {
        var copies = await _copyRepository.GetAllDeletedAsync();
        return copies.Select(MapToDto);
    }

    public async Task RestoreAsync(int id)
    {
        var copy = await _copyRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Copy with ID {id} not found.");
        if (!copy.IsDeleted)
            throw new BadRequestException($"Copy with ID {id} is not deleted.");
        await _copyRepository.RestoreAsync(copy);
    }

    public async Task HardDeleteAsync(int id)
    {
        var copy = await _copyRepository.GetByIdIncludingDeletedAsync(id)
            ?? throw new NotFoundException($"Copy with ID {id} not found.");
        if (copy.Loans?.Count > 0)
            throw new BadRequestException($"Copy with ID {id} cannot be permanently deleted because it has associated loan records.");
        await _copyRepository.HardDeleteAsync(copy);
    }

    private static CopyDto MapToDto(Copy copy) => new()
    {
        Id = copy.Id,
        BookId = copy.BookId,
        BookTitle = copy.Book?.Title ?? string.Empty,
        Barcode = copy.Barcode,
        Status = copy.Status,
        CreatedAt = copy.CreatedAt,
        UpdatedAt = copy.UpdatedAt,
        IsDeleted = copy.IsDeleted,
        DeletedAt = copy.DeletedAt,
        DeletedBy = copy.DeletedBy
    };
}

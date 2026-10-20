using System.Security.Claims;
using Project.Application.Common.ETag;
using Project.Application.Common.Exceptions;
using Project.Application.Common.Webhooks;
using Project.Infrastructure.Persistence;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;
using Project.Application.Services;

namespace Project.Infrastructure.Persistence;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICopyRepository _copyRepository;
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebhookEventPublisher _webhookEventPublisher;
    private readonly IETagService _etagService;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        ILoanRepository loanRepository,
        ICopyRepository copyRepository,
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IWebhookEventPublisher webhookEventPublisher,
        IETagService etagService,
        ILogger<LoanService> logger)
    {
        _loanRepository = loanRepository;
        _copyRepository = copyRepository;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _webhookEventPublisher = webhookEventPublisher;
        _etagService = etagService;
        _logger = logger;
    }

    public async Task<IEnumerable<LoanDto>> GetAllAsync()
    {
        var loans = await _loanRepository.GetAllAsync();
        return loans.Select(MapToDto);
    }

    public async Task<IEnumerable<LoanDto>> GetLoansForUserAsync(int userId)
    {
        var loans = await _loanRepository.GetByUserIdAsync(userId);
        return loans.Select(MapToDto);
    }

    public async Task<LoanDto> GetByIdAsync(int id)
    {
        var loan = await _loanRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Loan with ID {id} not found.");
        return MapToDto(loan);
    }

    public async Task<LoanDto> CreateAsync(LoanCreateValidator dto)
    {
        if (dto.DueDate <= dto.IssueDate)
            throw new BadRequestException("Due date must be after the issue date.");

        var copy = await _copyRepository.GetByIdAsync(dto.CopyId)
            ?? throw new NotFoundException($"Copy with ID {dto.CopyId} not found.");
        if (copy.Status != "Available")
            throw new BadRequestException($"Copy with ID {dto.CopyId} is not available for loan.");
        var hasActiveLoan = await _loanRepository.HasActiveLoanForCopyAsync(dto.CopyId);
        if (hasActiveLoan)
            throw new ConflictException($"Copy with ID {dto.CopyId} already has an active loan.");

        var loan = new Loan
        {
            CopyId = dto.CopyId,
            BorrowerName = dto.BorrowerName,
            UserId = GetCurrentUserRole() == "Member" ? GetCurrentUserId() : null,
            IssueDate = NormalizeToUtc(dto.IssueDate),
            DueDate = NormalizeToUtc(dto.DueDate),
            Status = "Active"
        };

        copy.Status = "Issued";

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _loanRepository.AddAsync(loan);
            await _copyRepository.UpdateAsync(copy);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.LoanCreated,
            new { loanId = loan.Id, userId = loan.UserId, bookId = copy.BookId, copyId = copy.Id });

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.CopyStatusChanged,
            new { copyId = copy.Id, bookId = copy.BookId, barcode = copy.Barcode, status = copy.Status });

        return new LoanDto
        {
            Id = loan.Id,
            CopyId = loan.CopyId,
            Barcode = copy.Barcode,
            BookTitle = copy.Book?.Title ?? string.Empty,
            BorrowerName = loan.BorrowerName,
            IssueDate = loan.IssueDate,
            DueDate = loan.DueDate,
            Status = loan.Status,
            UpdatedAt = loan.UpdatedAt
        };
    }

    private static DateTime NormalizeToUtc(DateTime dt) =>
        dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    public async Task UpdateAsync(int id, LoanUpdateValidator dto)
    {
        var loan = await _loanRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Loan with ID {id} not found.");
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), loan.Id, loan.UpdatedAt);
        var previousStatus = loan.Status;
        var previousDueDate = loan.DueDate;
        loan.CopyId = dto.CopyId;
        loan.BorrowerName = dto.BorrowerName;
        loan.IssueDate = dto.IssueDate;
        loan.DueDate = dto.DueDate;
        loan.Status = dto.Status;
        await _loanRepository.UpdateAsync(loan);

        if (loan.Status == "Active" && loan.DueDate > previousDueDate)
        {
            _webhookEventPublisher.TryPublish(
                WebhookEventTypes.LoanRenewed,
                new { loanId = loan.Id, userId = loan.UserId, bookId = loan.Copy?.BookId, copyId = loan.CopyId });
        }
    }

    public async Task ReturnLoanAsync(int id)
    {
        var loan = await _loanRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Loan with ID {id} not found.");
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), loan.Id, loan.UpdatedAt);
        if (loan.Status == "Returned")
            throw new BadRequestException($"Loan with ID {id} has already been returned.");
        loan.Status = "Returned";
        loan.ReturnDate = DateTime.UtcNow;
        var copy = await _copyRepository.GetByIdAsync(loan.CopyId)
            ?? throw new NotFoundException($"Copy with ID {loan.CopyId} not found.");
        copy.Status = "Available";
        await _copyRepository.UpdateAsync(copy);
        await _loanRepository.UpdateAsync(loan);

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.LoanReturned,
            new { loanId = loan.Id, userId = loan.UserId, bookId = copy.BookId, copyId = copy.Id });

        _webhookEventPublisher.TryPublish(
            WebhookEventTypes.CopyStatusChanged,
            new { copyId = copy.Id, bookId = copy.BookId, barcode = copy.Barcode, status = copy.Status });
    }

    private static LoanDto MapToDto(Loan loan) => new()
    {
        Id = loan.Id,
        CopyId = loan.CopyId,
        Barcode = loan.Copy?.Barcode ?? string.Empty,
        BookTitle = loan.Copy?.Book?.Title ?? string.Empty,
        BorrowerName = loan.BorrowerName,
        IssueDate = loan.IssueDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
        Status = loan.Status,
        UpdatedAt = loan.UpdatedAt
    };

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out var userId))
            return userId;
        return null;
    }

    private string? GetCurrentUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    private string? GetIfMatchHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.IfMatch.ToString();
    }
}

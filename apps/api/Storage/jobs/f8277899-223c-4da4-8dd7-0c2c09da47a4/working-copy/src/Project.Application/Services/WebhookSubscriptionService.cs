using Project.Application.Common.ETag;
using Project.Application.Common.Exceptions;
using Project.Application.Validators;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Project.Application.Common.Dtos;

namespace Project.Application.Services;

public class WebhookSubscriptionService : IWebhookSubscriptionService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookSignatureService _signatureService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IETagService _etagService;
    private readonly ILogger<WebhookSubscriptionService> _logger;

    public WebhookSubscriptionService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookSignatureService signatureService,
        IHttpContextAccessor httpContextAccessor,
        IETagService etagService,
        ILogger<WebhookSubscriptionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _signatureService = signatureService;
        _httpContextAccessor = httpContextAccessor;
        _etagService = etagService;
        _logger = logger;
    }

    public async Task<IEnumerable<WebhookSubscriptionDto>> GetAllAsync()
    {
        var subscriptions = await _subscriptionRepository.GetAllAsync();
        return subscriptions.Select(MapToDto);
    }

    public async Task<WebhookSubscriptionDto> GetByIdAsync(int id)
    {
        var subscription = await GetEntityOrThrow(id);
        return MapToDto(subscription);
    }

    public async Task<WebhookSubscriptionCreatedResponse> CreateAsync(WebhookSubscriptionCreateRequest request)
    {
        var secret = _signatureService.GenerateSecret();

        var subscription = new WebhookSubscription
        {
            CallbackUrl = request.CallbackUrl.Trim(),
            Secret = secret,
            EventTypes = request.EventTypes.Distinct().ToList(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _subscriptionRepository.AddAsync(subscription);

        _logger.LogInformation("Webhook subscription created. Id: {SubscriptionId}", subscription.Id);

        return new WebhookSubscriptionCreatedResponse
        {
            Subscription = MapToDto(subscription),
            Secret = secret
        };
    }

    public async Task<WebhookSubscriptionDto> UpdateAsync(int id, WebhookSubscriptionUpdateRequest request)
    {
        var subscription = await GetEntityOrThrow(id);
        _etagService.EnsurePreconditionMet(GetIfMatchHeader(), subscription.Id, subscription.UpdatedAt);

        if (request.CallbackUrl is not null)
            subscription.CallbackUrl = request.CallbackUrl.Trim();

        if (request.EventTypes is not null)
            subscription.EventTypes = request.EventTypes.Distinct().ToList();

        if (request.IsActive is not null)
            subscription.IsActive = request.IsActive.Value;

        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);

        _logger.LogInformation("Webhook subscription updated. Id: {SubscriptionId}", subscription.Id);

        return MapToDto(subscription);
    }

    public async Task DeleteAsync(int id)
    {
        var subscription = await GetEntityOrThrow(id);
        await _subscriptionRepository.DeleteAsync(subscription);

        _logger.LogInformation("Webhook subscription deleted. Id: {SubscriptionId}", id);
    }

    private async Task<WebhookSubscription> GetEntityOrThrow(int id)
    {
        return await _subscriptionRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Webhook subscription with ID {id} not found.");
    }

    private string? GetIfMatchHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers.IfMatch.ToString();
    }

    private static WebhookSubscriptionDto MapToDto(WebhookSubscription subscription) => new()
    {
        Id = subscription.Id,
        CallbackUrl = subscription.CallbackUrl,
        EventTypes = subscription.EventTypes,
        IsActive = subscription.IsActive,
        CreatedAt = subscription.CreatedAt,
        UpdatedAt = subscription.UpdatedAt
    };
}

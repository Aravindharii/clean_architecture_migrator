using LibraryManagement.Api.DTOs;

namespace LibraryManagement.Api.Services.Interfaces;

public interface IWebhookSubscriptionService
{
    Task<IEnumerable<WebhookSubscriptionDto>> GetAllAsync();

    Task<WebhookSubscriptionDto> GetByIdAsync(int id);

    Task<WebhookSubscriptionCreatedResponse> CreateAsync(WebhookSubscriptionCreateRequest request);

    Task<WebhookSubscriptionDto> UpdateAsync(int id, WebhookSubscriptionUpdateRequest request);

    Task DeleteAsync(int id);
}

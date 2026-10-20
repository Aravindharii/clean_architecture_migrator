using Project.Application.Validators;
using Project.Application.Common.Dtos;

namespace Project.Application.Common.Interfaces;

public interface IWebhookSubscriptionService
{
    Task<IEnumerable<WebhookSubscriptionDto>> GetAllAsync();

    Task<WebhookSubscriptionDto> GetByIdAsync(int id);

    Task<WebhookSubscriptionCreatedResponse> CreateAsync(WebhookSubscriptionCreateRequest request);

    Task<WebhookSubscriptionDto> UpdateAsync(int id, WebhookSubscriptionUpdateRequest request);

    Task DeleteAsync(int id);
}

using Project.Domain.Entities;

namespace Project.Application.Common.Interfaces;

public interface IWebhookSubscriptionRepository
{
    Task<IEnumerable<WebhookSubscription>> GetAllAsync();

    Task<WebhookSubscription?> GetByIdAsync(int id);

    Task<WebhookSubscription> AddAsync(WebhookSubscription subscription);

    Task UpdateAsync(WebhookSubscription subscription);

    Task DeleteAsync(WebhookSubscription subscription);

    Task<IEnumerable<WebhookSubscription>> GetActiveByEventTypeAsync(string eventType);
}

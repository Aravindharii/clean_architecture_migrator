using Project.Infrastructure.Common;
using Project.Infrastructure.Persistence;
using Project.Domain.Entities;
using Project.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence;

public class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<WebhookSubscriptionRepository> _logger;

    public WebhookSubscriptionRepository(AppDbContext context, ILogger<WebhookSubscriptionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<WebhookSubscription>> GetAllAsync()
    {
        return await _context.WebhookSubscriptions
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<WebhookSubscription?> GetByIdAsync(int id)
    {
        return await _context.WebhookSubscriptions.FindAsync(id);
    }

    public async Task<IEnumerable<WebhookSubscription>> GetActiveByEventTypeAsync(string eventType)
    {
        return await _context.WebhookSubscriptions
            .Where(w => w.IsActive)
            .Where(w => w.EventTypes.Contains(eventType))
            .ToListAsync();
    }

    public async Task<WebhookSubscription> AddAsync(WebhookSubscription subscription)
    {
        _context.WebhookSubscriptions.Add(subscription);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "adding", "WebhookSubscription", _logger);
        }

        return subscription;
    }

    public async Task UpdateAsync(WebhookSubscription subscription)
    {
        _context.WebhookSubscriptions.Update(subscription);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating WebhookSubscription {Id}", subscription.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "updating", "WebhookSubscription", _logger);
        }
    }

    public async Task DeleteAsync(WebhookSubscription subscription)
    {
        _context.WebhookSubscriptions.Remove(subscription);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error deleting WebhookSubscription {Id}", subscription.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            DatabaseExceptionHelper.HandleDbUpdateException(ex, "deleting", "WebhookSubscription", _logger);
        }
    }
}

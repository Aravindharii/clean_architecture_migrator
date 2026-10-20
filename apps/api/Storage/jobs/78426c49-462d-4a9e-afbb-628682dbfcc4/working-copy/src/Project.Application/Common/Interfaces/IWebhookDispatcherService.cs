namespace Project.Application.Common.Interfaces;

public interface IWebhookDispatcherService
{
    Task DispatchAsync(string eventType, object data, CancellationToken cancellationToken = default);
}

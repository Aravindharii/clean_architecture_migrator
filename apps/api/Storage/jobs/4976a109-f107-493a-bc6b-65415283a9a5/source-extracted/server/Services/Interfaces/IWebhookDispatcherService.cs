namespace LibraryManagement.Api.Services.Interfaces;

public interface IWebhookDispatcherService
{
    Task DispatchAsync(string eventType, object data, CancellationToken cancellationToken = default);
}

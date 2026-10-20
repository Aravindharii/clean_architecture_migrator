namespace LibraryManagement.Api.Services.Interfaces;

public interface IWebhookEventPublisher
{
    bool TryPublish(string eventType, object data);
}

namespace Project.Application.Common.Interfaces;

public interface IWebhookEventPublisher
{
    bool TryPublish(string eventType, object data);
}

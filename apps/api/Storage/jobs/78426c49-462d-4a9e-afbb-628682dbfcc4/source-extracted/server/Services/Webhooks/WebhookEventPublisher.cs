using System.Threading.Channels;
using LibraryManagement.Api.Common.Webhooks;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services.Webhooks;

public class WebhookEventPublisher : IWebhookEventPublisher
{
    private readonly ChannelWriter<WebhookEvent> _writer;
    private readonly ILogger<WebhookEventPublisher> _logger;

    public WebhookEventPublisher(ChannelWriter<WebhookEvent> writer, ILogger<WebhookEventPublisher> logger)
    {
        _writer = writer;
        _logger = logger;
    }

    public bool TryPublish(string eventType, object data)
    {
        if (_writer.TryWrite(new WebhookEvent(eventType, data)))
            return true;

        _logger.LogWarning("Webhook event queue is full. Dropping event {EventType}.", eventType);
        return false;
    }
}

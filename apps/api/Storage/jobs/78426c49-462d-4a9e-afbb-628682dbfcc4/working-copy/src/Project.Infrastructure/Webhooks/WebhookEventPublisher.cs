using System.Threading.Channels;
using Project.Application.Common.Webhooks;
using Project.Application.Common.Interfaces;
using Project.Application.Services;

namespace Project.Infrastructure.Webhooks;

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

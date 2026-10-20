using System.Threading.Channels;
using LibraryManagement.Api.Common.Webhooks;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services.Webhooks;

public class WebhookEventBackgroundService : BackgroundService
{
    private readonly ChannelReader<WebhookEvent> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookEventBackgroundService> _logger;

    public WebhookEventBackgroundService(
        ChannelReader<WebhookEvent> reader,
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookEventBackgroundService> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var webhookEvent in _reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IWebhookDispatcherService>();

            try
            {
                await dispatcher.DispatchAsync(webhookEvent.EventType, webhookEvent.Data, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to dispatch webhook event {EventType}.",
                    webhookEvent.EventType);
            }
        }
    }
}

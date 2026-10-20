using System.Net.Http.Headers;
using System.Text;
using Project.Application.Common.Webhooks;
using Project.Application.Common.Interfaces;
using Project.Application.Services;

namespace Project.Infrastructure.Webhooks;

public class WebhookDispatcherService : IWebhookDispatcherService
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookPayloadBuilder _payloadBuilder;
    private readonly IWebhookSignatureService _signatureService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDispatcherService> _logger;

    public WebhookDispatcherService(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookPayloadBuilder payloadBuilder,
        IWebhookSignatureService signatureService,
        HttpClient httpClient,
        ILogger<WebhookDispatcherService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _payloadBuilder = payloadBuilder;
        _signatureService = signatureService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        var subscriptions = (await _subscriptionRepository.GetActiveByEventTypeAsync(eventType)).ToList();

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions for event {EventType}.", eventType);
            return;
        }

        var payload = _payloadBuilder.Build(eventType, data);

        foreach (var subscription in subscriptions)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, subscription.CallbackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(WebhookHeaders.ContentType);
                request.Headers.TryAddWithoutValidation(WebhookHeaders.EventHeader, eventType);
                request.Headers.TryAddWithoutValidation(
                    WebhookHeaders.SignatureHeader,
                    _signatureService.ComputeSignature(subscription.Secret, payload));

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook dispatched successfully. CallbackUrl: {CallbackUrl}, Event: {EventType}, StatusCode: {StatusCode}",
                        subscription.CallbackUrl,
                        eventType,
                        (int)response.StatusCode);
                }
                else
                {
                    _logger.LogWarning(
                        "Webhook delivery failed. CallbackUrl: {CallbackUrl}, Event: {EventType}, StatusCode: {StatusCode}",
                        subscription.CallbackUrl,
                        eventType,
                        (int)response.StatusCode);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Webhook delivery error. CallbackUrl: {CallbackUrl}, Event: {EventType}",
                    subscription.CallbackUrl,
                    eventType);
            }
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Project.Application.Common.Interfaces;
using Project.Application.Services;

namespace Project.Infrastructure.Webhooks;

public class WebhookPayloadBuilder : IWebhookPayloadBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Build(string eventType, object data)
    {
        var payload = new WebhookPayloadEnvelope
        {
            Event = eventType,
            OccurredAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
            Data = data
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private sealed class WebhookPayloadEnvelope
    {
        public string Event { get; init; } = string.Empty;

        public string OccurredAt { get; init; } = string.Empty;

        public object Data { get; init; } = new object();
    }
}

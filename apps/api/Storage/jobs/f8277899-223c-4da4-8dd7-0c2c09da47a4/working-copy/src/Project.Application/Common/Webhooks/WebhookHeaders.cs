namespace Project.Application.Common.Webhooks;

public static class WebhookHeaders
{
    public const string EventHeader = "X-Library-Event";

    public const string SignatureHeader = "X-Library-Signature";

    public const string ContentType = "application/json";
}

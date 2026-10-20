namespace LibraryManagement.Api.Common.Webhooks;

public sealed record WebhookEvent(string EventType, object Data);

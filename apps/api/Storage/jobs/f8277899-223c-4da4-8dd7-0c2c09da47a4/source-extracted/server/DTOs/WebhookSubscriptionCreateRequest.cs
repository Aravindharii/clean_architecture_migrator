namespace LibraryManagement.Api.DTOs;

public class WebhookSubscriptionCreateRequest
{
    public string CallbackUrl { get; set; } = string.Empty;

    public List<string> EventTypes { get; set; } = new();
}

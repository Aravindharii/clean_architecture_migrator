namespace LibraryManagement.Api.DTOs;

public class WebhookSubscriptionCreatedResponse
{
    public WebhookSubscriptionDto Subscription { get; set; } = new();

    public string Secret { get; set; } = string.Empty;
}

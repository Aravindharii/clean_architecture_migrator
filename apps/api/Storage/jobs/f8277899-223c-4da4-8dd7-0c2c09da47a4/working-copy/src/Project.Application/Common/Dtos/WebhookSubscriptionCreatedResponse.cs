using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class WebhookSubscriptionCreatedResponse
{
    public WebhookSubscriptionDto Subscription { get; set; } = new();

    public string Secret { get; set; } = string.Empty;
}

using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class WebhookSubscriptionCreateRequest
{
    public string CallbackUrl { get; set; } = string.Empty;

    public List<string> EventTypes { get; set; } = new();
}

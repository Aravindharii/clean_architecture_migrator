using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class WebhookSubscriptionUpdateRequest
{
    public string? CallbackUrl { get; set; }

    public List<string>? EventTypes { get; set; }

    public bool? IsActive { get; set; }
}

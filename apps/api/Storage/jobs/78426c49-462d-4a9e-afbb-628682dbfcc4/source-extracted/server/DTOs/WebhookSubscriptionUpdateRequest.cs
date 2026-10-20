namespace LibraryManagement.Api.DTOs;

public class WebhookSubscriptionUpdateRequest
{
    public string? CallbackUrl { get; set; }

    public List<string>? EventTypes { get; set; }

    public bool? IsActive { get; set; }
}

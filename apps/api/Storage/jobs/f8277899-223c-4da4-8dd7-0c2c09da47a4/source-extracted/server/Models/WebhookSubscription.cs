namespace LibraryManagement.Api.Models;

public class WebhookSubscription
{
    public int Id { get; set; }

    public string CallbackUrl { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public List<string> EventTypes { get; set; } = new();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

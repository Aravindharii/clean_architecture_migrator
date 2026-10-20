using Project.Application.Validators;
namespace Project.Application.Common.Dtos;

public class WebhookSubscriptionDto
{
    public int Id { get; set; }

    public string CallbackUrl { get; set; } = string.Empty;

    public List<string> EventTypes { get; set; } = new();

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

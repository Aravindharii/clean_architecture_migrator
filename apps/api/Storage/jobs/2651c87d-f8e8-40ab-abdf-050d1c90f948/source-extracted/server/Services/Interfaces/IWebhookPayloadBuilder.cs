namespace LibraryManagement.Api.Services.Interfaces;

public interface IWebhookPayloadBuilder
{
    string Build(string eventType, object data);
}

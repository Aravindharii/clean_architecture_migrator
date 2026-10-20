namespace Project.Application.Common.Interfaces;

public interface IWebhookPayloadBuilder
{
    string Build(string eventType, object data);
}

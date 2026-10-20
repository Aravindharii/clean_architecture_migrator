namespace Project.Application.Common.Interfaces;

public interface IWebhookSignatureService
{
    string GenerateSecret();

    string ComputeSignature(string secret, string payload);
}

namespace LibraryManagement.Api.Services.Interfaces;

public interface IWebhookSignatureService
{
    string GenerateSecret();

    string ComputeSignature(string secret, string payload);
}

using System.Security.Cryptography;
using System.Text;
using LibraryManagement.Api.Services.Interfaces;

namespace LibraryManagement.Api.Services.Webhooks;

public class WebhookSignatureService : IWebhookSignatureService
{
    private const string SecretPrefix = "whsec_";
    private const int SecretKeySizeInBytes = 32;

    public string GenerateSecret()
    {
        var key = RandomNumberGenerator.GetBytes(SecretKeySizeInBytes);
        return SecretPrefix + Convert.ToHexString(key).ToLowerInvariant();
    }

    public string ComputeSignature(string secret, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

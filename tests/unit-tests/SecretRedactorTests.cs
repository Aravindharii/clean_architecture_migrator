using CleanArchitectureMigration.Api.Services;
using Xunit;

namespace CleanArchitectureMigration.Tests;

public class SecretRedactorTests
{
    [Fact]
    public void Redact_ConnectionStrings_MasksSensitiveCredentials()
    {
        var raw = "Server=myServer;Database=myDataBase;Uid=myUsername;Pwd=superSecretPassword123;";
        var redacted = SecretRedactor.Redact(raw);

        Assert.DoesNotContain("superSecretPassword123", redacted);
        Assert.Contains("Pwd=[REDACTED]", redacted);
    }

    [Fact]
    public void Redact_PostgresUrl_MasksPassword()
    {
        var raw = "postgres://admin:verySecretDbPass@postgres.internal:5432/appdb";
        var redacted = SecretRedactor.Redact(raw);

        Assert.DoesNotContain("verySecretDbPass", redacted);
        Assert.Contains("postgres://admin:[REDACTED]@", redacted);
    }

    [Fact]
    public void Redact_JwtBearerTokens_MasksToken()
    {
        var raw = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0";
        var redacted = SecretRedactor.Redact(raw);

        Assert.DoesNotContain("eyJzdWIiOiIxMjM0NTY3ODkwIn0", redacted);
        Assert.Contains("Bearer [REDACTED_JWT]", redacted);
    }
}

using System.Text.RegularExpressions;

namespace CleanArchitectureMigration.Api.Services;

public static class SecretRedactor
{
    private static readonly (Regex Pattern, string Replacement)[] RedactionRules =
    [
        // Database Connection strings with passwords
        (new Regex(@"(Password|pwd|User\s*Id|Uid)\s*=\s*[^;""'\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1=[REDACTED]"),
        (new Regex(@"postgres(ql)?:\/\/([^:]+):([^@]+)@", RegexOptions.IgnoreCase | RegexOptions.Compiled), "postgres://$2:[REDACTED]@"),
        (new Regex(@"mysql:\/\/([^:]+):([^@]+)@", RegexOptions.IgnoreCase | RegexOptions.Compiled), "mysql://$2:[REDACTED]@"),

        // API keys and JWT tokens
        (new Regex(@"(api[_-]?key|secret|token|apikey|client_secret)\s*[:=]\s*[""']?[A-Za-z0-9_\-\.]{16,}[""']?", RegexOptions.IgnoreCase | RegexOptions.Compiled), "$1=[REDACTED_SECRET]"),
        (new Regex(@"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Bearer [REDACTED_JWT]"),

        // Private keys
        (new Regex(@"-----BEGIN [A-Z ]+PRIVATE KEY-----[\s\S]*?-----END [A-Z ]+PRIVATE KEY-----", RegexOptions.Compiled), "[REDACTED_PRIVATE_KEY]"),

        // AWS / Cloud tokens
        (new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled), "[REDACTED_AWS_KEY]")
    ];

    public static string Redact(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input;
        foreach (var (pattern, replacement) in RedactionRules)
        {
            result = pattern.Replace(result, replacement);
        }
        return result;
    }
}

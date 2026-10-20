namespace CleanArchitectureMigration.Contracts;

public sealed class ValidationReport
{
    public Guid JobId { get; set; }
    public bool OverallSuccess { get; set; }
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;
    public BuildResult Build { get; set; } = new();
    public TestResult Tests { get; set; } = new();
    public List<RuleEvaluationResult> ArchitectureRules { get; set; } = [];
    public List<TransformationOperationResult> TransformationResults { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}

public enum ToolVerificationStatus
{
    Succeeded,
    Failed,
    ToolUnavailable
}

public sealed class BuildResult
{
    public bool Success { get; set; } = true;
    public bool ToolAvailable { get; set; } = true;
    public ToolVerificationStatus Status { get; set; } = ToolVerificationStatus.Succeeded;
    public int ErrorsCount { get; set; }
    public int WarningsCount { get; set; }
    public List<string> PreExistingErrors { get; set; } = [];
    public List<string> NewlyIntroducedErrors { get; set; } = [];
    public string RawOutput { get; set; } = string.Empty;
}

public sealed class TestResult
{
    public bool Success { get; set; } = true;
    public bool ToolAvailable { get; set; } = true;
    public ToolVerificationStatus Status { get; set; } = ToolVerificationStatus.Succeeded;
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> PreExistingFailures { get; set; } = [];
    public List<string> NewlyIntroducedFailures { get; set; } = [];
    public string RawOutput { get; set; } = string.Empty;
}

public sealed class RuleEvaluationResult
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public ViolationSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> OffendingSymbols { get; set; } = [];
}

public sealed class TransformationOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public OperationType Type { get; set; }
    public bool Success { get; set; }
    public string? AffectedPath { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

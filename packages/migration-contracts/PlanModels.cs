namespace CleanArchitectureMigration.Contracts;

public sealed class MigrationPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public int PlanVersion { get; set; } = 1;
    public string SourceHash { get; set; } = string.Empty;
    public TargetArchitecture TargetArchitecture { get; set; }
    public string Summary { get; set; } = string.Empty;
    public RiskLevel OverallRisk { get; set; } = RiskLevel.Medium;
    public double EstimatedEffortHours { get; set; } = 4.0;
    public List<PlanOperation> Operations { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PlanOperation
{
    public string OperationId { get; set; } = Guid.NewGuid().ToString("N");
    public int Sequence { get; set; }
    public OperationType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public string? DestinationPath { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
    public List<string> Evidence { get; set; } = [];
    public double Confidence { get; set; } = 0.95;
    public RiskLevel Risk { get; set; } = RiskLevel.Low;
    public List<string> Prerequisites { get; set; } = [];
    public bool RequiresApproval { get; set; } = true;
    public bool IsApproved { get; set; } = true;
    public bool ManualReviewRequired { get; set; }
    public string? CodeDiffPreview { get; set; }
}

public sealed class ApprovalRecord
{
    public Guid ApprovalId { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid JobId { get; set; }
    public string ApprovedBy { get; set; } = "developer";
    public string SourceHash { get; set; } = string.Empty;
    public int PlanVersion { get; set; }
    public DateTime ApprovedAtUtc { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
}

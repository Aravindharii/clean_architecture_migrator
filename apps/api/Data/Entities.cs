using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.Api.Data;

[Table("users")]
public class UserEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Developer";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ProjectEntity> Projects { get; set; } = [];
}

[Table("projects")]
public class ProjectEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public UserEntity? User { get; set; }

    public List<MigrationJobEntity> Jobs { get; set; } = [];
}

[Table("migration_jobs")]
public class MigrationJobEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Uploaded;
    public string SourceHash { get; set; } = string.Empty;
    public LanguageKind Language { get; set; } = LanguageKind.Unknown;
    public string Framework { get; set; } = string.Empty;
    public BuildSystemKind BuildSystem { get; set; } = BuildSystemKind.Unknown;
    public string TargetArchitecture { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? CurrentStep { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }

    public string? UploadedArchivePath { get; set; }
    public string? ExtractedSourcePath { get; set; }
    public string? MigratedOutputPath { get; set; }

    public AnalysisRunEntity? AnalysisRun { get; set; }
    public List<MigrationPlanEntity> Plans { get; set; } = [];
    public List<ApprovalEntity> Approvals { get; set; } = [];
    public TransformationRunEntity? TransformationRun { get; set; }
    public ValidationResultEntity? ValidationResult { get; set; }
    public List<ArtifactEntity> Artifacts { get; set; } = [];
    public List<AuditEventEntity> AuditEvents { get; set; } = [];
}

[Table("analysis_runs")]
public class AnalysisRunEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public string SourceHash { get; set; } = string.Empty;
    public string AnalysisResultJson { get; set; } = "{}";
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("migration_plans")]
public class MigrationPlanEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public int PlanVersion { get; set; } = 1;
    public string SourceHash { get; set; } = string.Empty;
    public string TargetArchitecture { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string OverallRisk { get; set; } = "medium";
    public string PlanJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

[Table("approvals")]
public class ApprovalEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public string ApprovedBy { get; set; } = string.Empty;
    public string SourceHash { get; set; } = string.Empty;
    public int PlanVersion { get; set; }
    public DateTime ApprovedAtUtc { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
}

[Table("transformation_runs")]
public class TransformationRunEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public bool Success { get; set; }
    public string OperationsResultJson { get; set; } = "[]";
}

[Table("validation_results")]
public class ValidationResultEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public bool OverallSuccess { get; set; }
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;
    public string ReportJson { get; set; } = "{}";
}

[Table("artifacts")]
public class ArtifactEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string RelativeStoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("audit_events")]
public class AuditEventEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public MigrationJobEntity? Job { get; set; }

    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string DetailsJson { get; set; } = "{}";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

namespace CleanArchitectureMigration.Contracts;

public sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class ProjectResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? LatestJobId { get; set; }
    public JobStatus? LatestJobStatus { get; set; }
}

public sealed class UploadArchiveResponse
{
    public Guid JobId { get; set; }
    public Guid ProjectId { get; set; }
    public string SourceHash { get; set; } = string.Empty;
    public long ArchiveSizeBytes { get; set; }
    public int FileCount { get; set; }
    public JobStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class JobStatusResponse
{
    public Guid JobId { get; set; }
    public Guid ProjectId { get; set; }
    public JobStatus Status { get; set; }
    public string SourceHash { get; set; } = string.Empty;
    public LanguageKind Language { get; set; }
    public string Framework { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CurrentStep { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? PlanId { get; set; }
    public bool HasApproval { get; set; }
}

public sealed class CreatePlanRequest
{
    public string? CustomInstructions { get; set; }
}

public sealed class UpdatePlanRequest
{
    public string? Summary { get; set; }
    public List<PlanOperation>? Operations { get; set; }
}

public sealed class ApprovePlanRequest
{
    public string SourceHash { get; set; } = string.Empty;
    public int PlanVersion { get; set; }
    public string ApprovedBy { get; set; } = "developer";
    public string Notes { get; set; } = string.Empty;
}

public sealed class ArtifactMetadataResponse
{
    public Guid ArtifactId { get; set; }
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

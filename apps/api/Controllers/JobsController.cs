using System.Text.Json;
using CleanArchitectureMigration.Api.Data;
using CleanArchitectureMigration.Api.Services;
using CleanArchitectureMigration.Contracts;
using CleanArchitectureMigration.Orchestrator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureMigration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly MigrationDbContext _db;
    private readonly IMigrationOrchestrator _orchestrator;
    private readonly IJobQueue _jobQueue;
    private readonly IArtifactStorageService _storageService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        MigrationDbContext db,
        IMigrationOrchestrator orchestrator,
        IJobQueue jobQueue,
        IArtifactStorageService storageService,
        ILogger<JobsController> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _jobQueue = jobQueue;
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(Guid jobId)
    {
        var job = await _db.MigrationJobs
            .Include(j => j.Plans)
            .Include(j => j.Approvals)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null) return NotFound(new { error = $"Job {jobId} not found." });

        var latestPlan = job.Plans.OrderByDescending(p => p.PlanVersion).FirstOrDefault();
        var hasActiveApproval = job.Approvals.Any(a => !a.IsRevoked && a.PlanVersion == latestPlan?.PlanVersion);

        return Ok(new JobStatusResponse
        {
            JobId = job.Id,
            ProjectId = job.ProjectId,
            Status = job.Status,
            SourceHash = job.SourceHash,
            Language = job.Language,
            Framework = job.Framework,
            CreatedAtUtc = job.CreatedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            CurrentStep = job.CurrentStep,
            ProgressPercent = job.ProgressPercent,
            ErrorMessage = job.ErrorMessage,
            PlanId = latestPlan?.Id,
            HasApproval = hasActiveApproval
        });
    }

    [HttpGet("{jobId}/analysis")]
    public async Task<ActionResult<ProjectAnalysisResult>> GetAnalysis(Guid jobId)
    {
        var analysisRun = await _db.AnalysisRuns.FirstOrDefaultAsync(a => a.JobId == jobId);
        if (analysisRun == null)
        {
            return NotFound(new { error = $"Analysis for job {jobId} is not available yet." });
        }

        var result = JsonSerializer.Deserialize<ProjectAnalysisResult>(analysisRun.AnalysisResultJson);
        return Ok(result);
    }

    [HttpPost("{jobId}/plans")]
    public async Task<ActionResult> RequestPlanRegeneration(Guid jobId, [FromBody] CreatePlanRequest? request)
    {
        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return NotFound(new { error = $"Job {jobId} not found." });

        if (job.Status != JobStatus.AnalysisReady && job.Status != JobStatus.AwaitingApproval && job.Status != JobStatus.Planning)
        {
            return BadRequest(new { error = $"Cannot generate plan in status {job.Status}." });
        }

        // Revoke any previous approvals since plan will regenerate
        var approvals = await _db.Approvals.Where(a => a.JobId == jobId && !a.IsRevoked).ToListAsync();
        foreach (var a in approvals)
        {
            a.IsRevoked = true;
            a.RevokedReason = "New plan version requested.";
        }

        job.Status = JobStatus.Planning;
        job.CurrentStep = "Regenerating Clean Architecture migration plan...";
        await _db.SaveChangesAsync();

        await _jobQueue.EnqueueAsync(new JobMessage(jobId, JobAction.RunPlanning, request?.CustomInstructions));

        return Accepted(new { message = "Plan generation enqueued." });
    }

    [HttpGet("{jobId}/plans/{planId}")]
    public async Task<ActionResult<MigrationPlan>> GetPlan(Guid jobId, Guid planId)
    {
        var planEntity = await _db.MigrationPlans.FirstOrDefaultAsync(p => p.JobId == jobId && p.Id == planId);
        if (planEntity == null)
        {
            return NotFound(new { error = $"Plan {planId} for job {jobId} not found." });
        }

        var plan = JsonSerializer.Deserialize<MigrationPlan>(planEntity.PlanJson);
        return Ok(plan);
    }

    [HttpPatch("{jobId}/plans/{planId}")]
    public async Task<ActionResult<MigrationPlan>> UpdatePlan(Guid jobId, Guid planId, [FromBody] UpdatePlanRequest request)
    {
        var planEntity = await _db.MigrationPlans.FirstOrDefaultAsync(p => p.JobId == jobId && p.Id == planId);
        if (planEntity == null)
        {
            return NotFound(new { error = $"Plan {planId} not found." });
        }

        var plan = JsonSerializer.Deserialize<MigrationPlan>(planEntity.PlanJson)
                   ?? throw new InvalidOperationException("Failed to deserialize existing plan.");

        if (request.Summary != null) plan.Summary = request.Summary;
        if (request.Operations != null) plan.Operations = request.Operations;

        plan.UpdatedAtUtc = DateTime.UtcNow;
        planEntity.Summary = plan.Summary;
        planEntity.PlanJson = JsonSerializer.Serialize(plan);
        planEntity.UpdatedAtUtc = DateTime.UtcNow;

        // Revoke any existing approvals since plan was modified
        var activeApprovals = await _db.Approvals.Where(a => a.JobId == jobId && !a.IsRevoked).ToListAsync();
        foreach (var a in activeApprovals)
        {
            a.IsRevoked = true;
            a.RevokedReason = "Plan modified by developer.";
        }

        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job != null && job.Status == JobStatus.Approved)
        {
            job.Status = JobStatus.AwaitingApproval;
            job.CurrentStep = "Plan modified. Re-approval required before transformation.";
        }

        await _db.SaveChangesAsync();
        return Ok(plan);
    }

    [HttpPost("{jobId}/plans/{planId}/approve")]
    public async Task<ActionResult<ApprovalRecord>> ApprovePlan(Guid jobId, Guid planId, [FromBody] ApprovePlanRequest request)
    {
        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return NotFound(new { error = $"Job {jobId} not found." });

        var planEntity = await _db.MigrationPlans.FirstOrDefaultAsync(p => p.JobId == jobId && p.Id == planId);
        if (planEntity == null) return NotFound(new { error = $"Plan {planId} not found." });

        var plan = JsonSerializer.Deserialize<MigrationPlan>(planEntity.PlanJson)
                   ?? throw new InvalidOperationException("Failed to deserialize plan.");

        try
        {
            // Verify cryptographically bound approval: checks SourceHash and PlanVersion
            var approvalRecord = await _orchestrator.VerifyAndRecordApprovalAsync(jobId, plan, job.SourceHash, request);

            var approvalEntity = new ApprovalEntity
            {
                Id = approvalRecord.ApprovalId,
                PlanId = plan.PlanId,
                JobId = jobId,
                ApprovedBy = approvalRecord.ApprovedBy,
                SourceHash = approvalRecord.SourceHash,
                PlanVersion = approvalRecord.PlanVersion,
                ApprovedAtUtc = approvalRecord.ApprovedAtUtc,
                Notes = approvalRecord.Notes
            };

            _db.Approvals.Add(approvalEntity);

            job.Status = JobStatus.Approved;
            job.CurrentStep = "Plan approved by developer. Ready to apply transformations.";

            _db.AuditEvents.Add(new AuditEventEntity
            {
                JobId = jobId,
                Action = "PlanApproved",
                PerformedBy = approvalRecord.ApprovedBy,
                DetailsJson = JsonSerializer.Serialize(new { PlanId = plan.PlanId, Version = plan.PlanVersion, Hash = approvalRecord.SourceHash }),
                TimestampUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(approvalRecord);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Approval rejected for job {JobId}: {Reason}", jobId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{jobId}/transformations")]
    public async Task<ActionResult> ApplyTransformations(Guid jobId)
    {
        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return NotFound(new { error = $"Job {jobId} not found." });

        // Enforce explicit developer approval
        if (job.Status != JobStatus.Approved)
        {
            return BadRequest(new { error = $"Explicit developer approval is required before applying transformations. Current status is '{job.Status}'." });
        }

        job.CurrentStep = "Transformation enqueued...";
        await _db.SaveChangesAsync();

        await _jobQueue.EnqueueAsync(new JobMessage(jobId, JobAction.RunTransformationAndValidation));

        return Accepted(new { message = "Transformations and validation scheduled." });
    }

    [HttpGet("{jobId}/validation")]
    public async Task<ActionResult<ValidationReport>> GetValidationReport(Guid jobId)
    {
        var val = await _db.ValidationResults.FirstOrDefaultAsync(v => v.JobId == jobId);
        if (val == null)
        {
            return NotFound(new { error = $"Validation report for job {jobId} is not available." });
        }

        var report = JsonSerializer.Deserialize<ValidationReport>(val.ReportJson);
        return Ok(report);
    }

    [HttpGet("{jobId}/artifacts")]
    public async Task<ActionResult<List<ArtifactMetadataResponse>>> GetArtifacts(Guid jobId)
    {
        var artifacts = await _db.Artifacts
            .Where(a => a.JobId == jobId)
            .Select(a => new ArtifactMetadataResponse
            {
                ArtifactId = a.Id,
                JobId = a.JobId,
                FileName = a.FileName,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                Sha256 = a.Sha256,
                CreatedAtUtc = a.CreatedAtUtc,
                DownloadUrl = $"/api/jobs/{jobId}/artifacts/output.zip"
            })
            .ToListAsync();

        return Ok(artifacts);
    }

    [HttpGet("{jobId}/artifacts/output.zip")]
    public IActionResult DownloadOutputZip(Guid jobId)
    {
        var zipPath = _storageService.GetOutputZipPath(jobId);
        if (!System.IO.File.Exists(zipPath))
        {
            return NotFound(new { error = $"Migrated output ZIP for job {jobId} not found." });
        }

        var stream = System.IO.File.OpenRead(zipPath);
        return File(stream, "application/zip", $"clean-architecture-migrated-{jobId:N}.zip");
    }

    [HttpPost("{jobId}/cancel")]
    public async Task<ActionResult> CancelJob(Guid jobId)
    {
        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job == null) return NotFound(new { error = $"Job {jobId} not found." });

        if (job.Status == JobStatus.Completed || job.Status == JobStatus.Failed || job.Status == JobStatus.Cancelled)
        {
            return BadRequest(new { error = $"Job cannot be cancelled in state '{job.Status}'." });
        }

        job.Status = JobStatus.Cancelled;
        job.CurrentStep = "Job cancelled by developer.";
        job.CompletedAtUtc = DateTime.UtcNow;

        _db.AuditEvents.Add(new AuditEventEntity
        {
            JobId = jobId,
            Action = "JobCancelled",
            PerformedBy = "Developer",
            DetailsJson = "{}",
            TimestampUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { message = $"Job {jobId} cancelled." });
    }
}

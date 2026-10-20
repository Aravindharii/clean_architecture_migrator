using System.Text.Json;
using CleanArchitectureMigration.Api.Data;
using CleanArchitectureMigration.Contracts;
using CleanArchitectureMigration.Orchestrator;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureMigration.Api.Services;

public sealed class JobProcessorBackgroundService : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobProcessorBackgroundService> _logger;

    public JobProcessorBackgroundService(
        IJobQueue queue,
        IServiceProvider serviceProvider,
        ILogger<JobProcessorBackgroundService> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobProcessorBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _queue.DequeueAsync(stoppingToken);
                await ProcessMessageAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing job queue message.");
            }
        }

        _logger.LogInformation("JobProcessorBackgroundService stopping.");
    }

    private async Task ProcessMessageAsync(JobMessage message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MigrationDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IMigrationOrchestrator>();
        var storage = scope.ServiceProvider.GetRequiredService<IArtifactStorageService>();

        var job = await db.MigrationJobs
            .Include(j => j.Plans)
            .Include(j => j.Approvals)
            .FirstOrDefaultAsync(j => j.Id == message.JobId, ct);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for queue processing.", message.JobId);
            return;
        }

        try
        {
            switch (message.Action)
            {
                case JobAction.RunAnalysis:
                    await HandleRunAnalysisAsync(job, db, orchestrator, storage, ct);
                    break;

                case JobAction.RunPlanning:
                    await HandleRunPlanningAsync(job, db, orchestrator, message.ExtraData, ct);
                    break;

                case JobAction.RunTransformationAndValidation:
                    await HandleRunTransformationAndValidationAsync(job, db, orchestrator, storage, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId} during action {Action}", job.Id, message.Action);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CurrentStep = "Failed";
            job.CompletedAtUtc = DateTime.UtcNow;

            db.AuditEvents.Add(new AuditEventEntity
            {
                JobId = job.Id,
                Action = "JobFailed",
                PerformedBy = "System",
                DetailsJson = JsonSerializer.Serialize(new { Error = ex.Message, Stack = ex.StackTrace }),
                TimestampUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
        }
    }

    private async Task HandleRunAnalysisAsync(
        MigrationJobEntity job,
        MigrationDbContext db,
        IMigrationOrchestrator orchestrator,
        IArtifactStorageService storage,
        CancellationToken ct)
    {
        job.Status = JobStatus.Analyzing;
        job.CurrentStep = "Extracting source AST, symbols and project dependencies...";
        job.ProgressPercent = 15;
        await db.SaveChangesAsync(ct);

        var extractedDir = storage.GetExtractedSourcePath(job.Id);
        var analysis = await orchestrator.ExecuteAnalysisAsync(job.Id, extractedDir, job.ProjectId, job.SourceHash, ct);

        job.Language = analysis.Language;
        job.Framework = analysis.Framework;
        job.BuildSystem = analysis.BuildSystem;

        var analysisRun = new AnalysisRunEntity
        {
            JobId = job.Id,
            SourceHash = job.SourceHash,
            AnalysisResultJson = JsonSerializer.Serialize(analysis),
            AnalyzedAtUtc = DateTime.UtcNow
        };
        db.AnalysisRuns.Add(analysisRun);

        job.Status = JobStatus.AnalysisReady;
        job.CurrentStep = "Static analysis complete. Initiating AI clean architecture planning...";
        job.ProgressPercent = 35;
        await db.SaveChangesAsync(ct);

        // Transition directly into planning
        await HandleRunPlanningAsync(job, db, orchestrator, null, ct);
    }

    private async Task HandleRunPlanningAsync(
        MigrationJobEntity job,
        MigrationDbContext db,
        IMigrationOrchestrator orchestrator,
        string? customInstructions,
        CancellationToken ct)
    {
        job.Status = JobStatus.Planning;
        job.CurrentStep = "Generating structured Clean Architecture migration plan with AI...";
        job.ProgressPercent = 45;
        await db.SaveChangesAsync(ct);

        var analysisRun = await db.AnalysisRuns.FirstOrDefaultAsync(a => a.JobId == job.Id, ct);
        if (analysisRun == null)
        {
            throw new InvalidOperationException("Cannot generate plan: Analysis run missing.");
        }

        var analysis = JsonSerializer.Deserialize<ProjectAnalysisResult>(analysisRun.AnalysisResultJson)
                       ?? throw new InvalidOperationException("Failed to deserialize analysis results.");

        var plan = await orchestrator.ExecutePlanningAsync(job.Id, analysis, customInstructions, ct);

        var existingPlansCount = await db.MigrationPlans.CountAsync(p => p.JobId == job.Id, ct);
        plan.PlanVersion = existingPlansCount + 1;

        var planEntity = new MigrationPlanEntity
        {
            Id = plan.PlanId,
            JobId = job.Id,
            PlanVersion = plan.PlanVersion,
            SourceHash = job.SourceHash,
            TargetArchitecture = plan.TargetArchitecture.ToString(),
            Summary = plan.Summary,
            OverallRisk = plan.OverallRisk.ToString().ToLowerInvariant(),
            PlanJson = JsonSerializer.Serialize(plan),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.MigrationPlans.Add(planEntity);

        job.Status = JobStatus.AwaitingApproval;
        job.CurrentStep = "Migration plan generated. Awaiting explicit developer review and cryptographic approval.";
        job.ProgressPercent = 50;

        db.AuditEvents.Add(new AuditEventEntity
        {
            JobId = job.Id,
            Action = "PlanGenerated",
            PerformedBy = "AI Planner",
            DetailsJson = JsonSerializer.Serialize(new { PlanId = plan.PlanId, Version = plan.PlanVersion, OperationsCount = plan.Operations.Count }),
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleRunTransformationAndValidationAsync(
        MigrationJobEntity job,
        MigrationDbContext db,
        IMigrationOrchestrator orchestrator,
        IArtifactStorageService storage,
        CancellationToken ct)
    {
        // Enforce strict approval requirement
        if (job.Status != JobStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot execute transformations: Job {job.Id} is not approved (Status is {job.Status}).");
        }

        var latestPlanEntity = await db.MigrationPlans
            .Where(p => p.JobId == job.Id)
            .OrderByDescending(p => p.PlanVersion)
            .FirstOrDefaultAsync(ct) ?? throw new InvalidOperationException("Migration plan not found.");

        var plan = JsonSerializer.Deserialize<MigrationPlan>(latestPlanEntity.PlanJson)
                   ?? throw new InvalidOperationException("Failed to deserialize migration plan.");

        job.Status = JobStatus.Transforming;
        job.CurrentStep = "Creating isolated working copy and applying approved transformations...";
        job.ProgressPercent = 65;
        await db.SaveChangesAsync(ct);

        // Create isolated working copy — NEVER modify original source
        var workingDir = await storage.CreateWorkingCopyAsync(job.Id, ct);
        job.MigratedOutputPath = workingDir;

        var transformResults = await orchestrator.ExecuteTransformationsAsync(job.Id, workingDir, plan, job.Language, ct);

        var transformRun = new TransformationRunEntity
        {
            JobId = job.Id,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            Success = transformResults.All(r => r.Success),
            OperationsResultJson = JsonSerializer.Serialize(transformResults)
        };
        db.TransformationRuns.Add(transformRun);

        job.Status = JobStatus.Validating;
        job.CurrentStep = "Building migrated project, running unit tests, and evaluating architectural rules...";
        job.ProgressPercent = 85;
        await db.SaveChangesAsync(ct);

        var validationReport = await orchestrator.ExecuteValidationAsync(job.Id, workingDir, job.Language, transformResults, ct);

        var validationEntity = new ValidationResultEntity
        {
            JobId = job.Id,
            OverallSuccess = validationReport.OverallSuccess,
            ValidatedAtUtc = DateTime.UtcNow,
            ReportJson = JsonSerializer.Serialize(validationReport)
        };
        db.ValidationResults.Add(validationEntity);

        // Package output ZIP
        var outputZipPath = await storage.PackageOutputZipAsync(job.Id, workingDir, ct);
        var zipInfo = new FileInfo(outputZipPath);

        string zipHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        using (var stream = File.OpenRead(outputZipPath))
        {
            zipHash = Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        var artifact = new ArtifactEntity
        {
            JobId = job.Id,
            FileName = "migrated-clean-architecture.zip",
            ContentType = "application/zip",
            RelativeStoragePath = outputZipPath,
            SizeBytes = zipInfo.Length,
            Sha256 = zipHash,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Artifacts.Add(artifact);

        job.Status = JobStatus.Completed;
        job.CurrentStep = "Migration completed successfully. Migrated solution is available for download.";
        job.ProgressPercent = 100;
        job.CompletedAtUtc = DateTime.UtcNow;

        db.AuditEvents.Add(new AuditEventEntity
        {
            JobId = job.Id,
            Action = "MigrationCompleted",
            PerformedBy = "System",
            DetailsJson = JsonSerializer.Serialize(new { OutputZip = artifact.FileName, OverallSuccess = validationReport.OverallSuccess }),
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}

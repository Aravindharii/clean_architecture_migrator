using CleanArchitectureMigration.Api.Data;
using CleanArchitectureMigration.Api.Services;
using CleanArchitectureMigration.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitectureMigration.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId}/uploads")]
public class UploadsController : ControllerBase
{
    private readonly MigrationDbContext _db;
    private readonly ISafeArchiveExtractor _archiveExtractor;
    private readonly IArtifactStorageService _storageService;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        MigrationDbContext db,
        ISafeArchiveExtractor archiveExtractor,
        IArtifactStorageService storageService,
        IJobQueue jobQueue,
        ILogger<UploadsController> logger)
    {
        _db = db;
        _archiveExtractor = archiveExtractor;
        _storageService = storageService;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(55 * 1024 * 1024)] // 55MB request limit
    public async Task<ActionResult<UploadArchiveResponse>> UploadArchive(
        Guid projectId,
      IFormFile? file,
        CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project == null)
        {
            return NotFound(new { error = $"Project {projectId} not found." });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided for upload." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".zip")
        {
            return BadRequest(new { error = "Only .zip archive files are accepted." });
        }

        var jobId = Guid.NewGuid();
        var extractedDir = _storageService.GetExtractedSourcePath(jobId);

        // Buffer the entire upload so it can be read twice (extraction + archival)
        // This also handles non-seekable multipart streams from ASP.NET Core form parsing.
        using var bufferedStream = new MemoryStream();
        using (var rawStream = file.OpenReadStream())
        {
            await rawStream.CopyToAsync(bufferedStream, cancellationToken);
        }
        bufferedStream.Position = 0;

        // Safe extraction with security controls (Zip Slip, zip bomb, symlink checks)
        var extraction = await _archiveExtractor.ExtractSafelyAsync(bufferedStream, extractedDir, cancellationToken);

        if (!extraction.Success)
        {
            _logger.LogWarning("Safe archive extraction failed for project {ProjectId}: {Error}", projectId, extraction.ErrorMessage);
            // Clean up partial extraction on failure
            if (Directory.Exists(extractedDir)) Directory.Delete(extractedDir, true);
            return BadRequest(new { error = extraction.ErrorMessage });
        }

        // Store original uploaded ZIP for safe archival (re-read buffered bytes)
        bufferedStream.Position = 0;
        await _storageService.SaveUploadedZipAsync(jobId, bufferedStream, cancellationToken);

        var job = new MigrationJobEntity
        {
            Id = jobId,
            ProjectId = projectId,
            Status = JobStatus.Uploaded,
            SourceHash = extraction.Sha256,
            CreatedAtUtc = DateTime.UtcNow,
            CurrentStep = "Archive uploaded and safely extracted.",
            ProgressPercent = 5,
            UploadedArchivePath = _storageService.GetUploadedZipPath(jobId),
            ExtractedSourcePath = extractedDir
        };

        _db.MigrationJobs.Add(job);
        _db.AuditEvents.Add(new AuditEventEntity
        {
            JobId = jobId,
            Action = "ArchiveUploaded",
            PerformedBy = "Developer",
            DetailsJson = $"{{\"fileName\":\"{file.FileName}\",\"size\":{extraction.ArchiveSizeBytes},\"sha256\":\"{extraction.Sha256}\",\"files\":{extraction.FileCount}}}",
            TimestampUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        // Enqueue background analysis & planning
        await _jobQueue.EnqueueAsync(new JobMessage(jobId, JobAction.RunAnalysis), cancellationToken);

        return Ok(new UploadArchiveResponse
        {
            JobId = jobId,
            ProjectId = projectId,
            SourceHash = extraction.Sha256,
            ArchiveSizeBytes = extraction.ArchiveSizeBytes,
            FileCount = extraction.FileCount,
            Status = JobStatus.Uploaded,
            Message = "Archive uploaded safely. Analysis started in background."
        });
    }
}

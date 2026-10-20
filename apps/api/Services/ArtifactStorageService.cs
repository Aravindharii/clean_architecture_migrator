using System.IO.Compression;
using System.Security.Cryptography;
using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.Api.Services;

public interface IArtifactStorageService
{
    string GetJobDirectory(Guid jobId);
    string GetUploadedZipPath(Guid jobId);
    string GetExtractedSourcePath(Guid jobId);
    string GetWorkingCopyPath(Guid jobId);
    string GetOutputZipPath(Guid jobId);

    Task SaveUploadedZipAsync(Guid jobId, Stream stream, CancellationToken cancellationToken = default);
    Task<string> CreateWorkingCopyAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<string> PackageOutputZipAsync(Guid jobId, string transformedDirectory, CancellationToken cancellationToken = default);
    Task CleanJobDirectoryAsync(Guid jobId);
}

public sealed class LocalArtifactStorageService : IArtifactStorageService
{
    private readonly string _baseStoragePath;

    public LocalArtifactStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"];
        _baseStoragePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "Storage")
            : configuredPath;

        if (!Directory.Exists(_baseStoragePath))
        {
            Directory.CreateDirectory(_baseStoragePath);
        }
    }

    public string GetJobDirectory(Guid jobId) => Path.Combine(_baseStoragePath, "jobs", jobId.ToString());
    public string GetUploadedZipPath(Guid jobId) => Path.Combine(GetJobDirectory(jobId), "source-original.zip");
    public string GetExtractedSourcePath(Guid jobId) => Path.Combine(GetJobDirectory(jobId), "source-extracted");
    public string GetWorkingCopyPath(Guid jobId) => Path.Combine(GetJobDirectory(jobId), "working-copy");
    public string GetOutputZipPath(Guid jobId) => Path.Combine(GetJobDirectory(jobId), "migrated-clean-architecture.zip");

    public async Task SaveUploadedZipAsync(Guid jobId, Stream stream, CancellationToken cancellationToken = default)
    {
        var jobDir = GetJobDirectory(jobId);
        Directory.CreateDirectory(jobDir);
        var zipPath = GetUploadedZipPath(jobId);

        using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<string> CreateWorkingCopyAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var sourceDir = GetExtractedSourcePath(jobId);
        var workingDir = GetWorkingCopyPath(jobId);

        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Source extracted directory not found for job {jobId}");
        }

        if (Directory.Exists(workingDir))
        {
            Directory.Delete(workingDir, true);
        }

        CopyDirectory(sourceDir, workingDir);
        return Task.FromResult(workingDir);
    }

    public async Task<string> PackageOutputZipAsync(Guid jobId, string transformedDirectory, CancellationToken cancellationToken = default)
    {
        var outputZip = GetOutputZipPath(jobId);
        if (File.Exists(outputZip))
        {
            File.Delete(outputZip);
        }

        await Task.Run(() =>
        {
            ZipFile.CreateFromDirectory(transformedDirectory, outputZip, CompressionLevel.Optimal, false);
        }, cancellationToken);

        return outputZip;
    }

    public Task CleanJobDirectoryAsync(Guid jobId)
    {
        var jobDir = GetJobDirectory(jobId);
        if (Directory.Exists(jobDir))
        {
            Directory.Delete(jobDir, true);
        }
        return Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }   
}

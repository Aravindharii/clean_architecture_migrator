using System.IO.Compression;
using System.Security.Cryptography;

namespace CleanArchitectureMigration.Api.Services;

public sealed class ArchiveExtractionResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long ArchiveSizeBytes { get; set; }
    public long TotalUncompressedBytes { get; set; }
    public int FileCount { get; set; }
    public string DestinationDirectory { get; set; } = string.Empty;
}

public interface ISafeArchiveExtractor
{
    Task<ArchiveExtractionResult> ExtractSafelyAsync(
        Stream archiveStream,
        string destinationDir,
        CancellationToken cancellationToken = default);
}

public sealed class SafeArchiveExtractor : ISafeArchiveExtractor
{
    // Security thresholds
    public const long MaxArchiveSizeBytes = 50 * 1024 * 1024;       // 50 MB
    public const long MaxUncompressedBytes = 250 * 1024 * 1024;    // 250 MB
    public const int MaxFileCount = 2500;
    public const double MaxCompressionRatio = 100.0;

    public async Task<ArchiveExtractionResult> ExtractSafelyAsync(
        Stream archiveStream,
        string destinationDir,
        CancellationToken cancellationToken = default)
    {
        var result = new ArchiveExtractionResult
        {
            DestinationDirectory = destinationDir
        };

        // Ensure clean destination directory
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        var canonicalDestination = Path.GetFullPath(destinationDir);
        if (!canonicalDestination.EndsWith(Path.DirectorySeparatorChar))
        {
            canonicalDestination += Path.DirectorySeparatorChar;
        }

        // Calculate SHA-256 while caching into memory or temp buffer
        using var memoryStream = new MemoryStream();
        await archiveStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        if (memoryStream.Length > MaxArchiveSizeBytes)
        {
            result.Success = false;
            result.ErrorMessage = $"Archive size ({memoryStream.Length} bytes) exceeds limit of {MaxArchiveSizeBytes} bytes.";
            return result;
        }

        result.ArchiveSizeBytes = memoryStream.Length;
        using (var sha = SHA256.Create())
        {
            var hashBytes = sha.ComputeHash(memoryStream);
            result.Sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        memoryStream.Position = 0;

        try
        {
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            if (archive.Entries.Count > MaxFileCount)
            {
                result.Success = false;
                result.ErrorMessage = $"Archive contains {archive.Entries.Count} files, exceeding limit of {MaxFileCount}.";
                return result;
            }

            long totalExtracted = 0;
            int fileCount = 0;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Path traversal check (Zip Slip vulnerability prevention)
                var destinationFilePath = Path.GetFullPath(Path.Combine(canonicalDestination, entry.FullName));
                if (!destinationFilePath.StartsWith(canonicalDestination, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Potential directory traversal attack detected in entry: '{entry.FullName}'";
                    return result;
                }

                // Check for Unix symlink / hardlink attributes if available
                // ExternalAttributes in Unix: upper 16 bits contain file permissions/type. 0120000 octal = 0xA000 = symlink
                if ((entry.ExternalAttributes >> 16 & 0xF000) == 0xA000)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Symlink entries are prohibited for security: '{entry.FullName}'";
                    return result;
                }

                // If entry is a directory
                if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                {
                    Directory.CreateDirectory(destinationFilePath);
                    continue;
                }

                // Compression ratio check for zip bomb protection
                if (entry.CompressedLength > 0)
                {
                    var ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > MaxCompressionRatio && entry.Length > 10 * 1024 * 1024)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Suspicious compression ratio ({ratio:F1}:1) in entry '{entry.FullName}'. Possible zip bomb.";
                        return result;
                    }
                }

                totalExtracted += entry.Length;
                if (totalExtracted > MaxUncompressedBytes)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Total uncompressed size exceeds limit of {MaxUncompressedBytes} bytes.";
                    return result;
                }

                var parentDir = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                // Write file to disk
                using var entryStream = entry.Open();
                using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await entryStream.CopyToAsync(fileStream, cancellationToken);

                fileCount++;
            }

            result.Success = true;
            result.TotalUncompressedBytes = totalExtracted;
            result.FileCount = fileCount;
            return result;
        }
        catch (InvalidDataException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Invalid or corrupt ZIP archive: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error extracting archive: {ex.Message}";
            return result;
        }
    }
}

using System.IO.Compression;
using System.Text;
using CleanArchitectureMigration.Api.Services;
using Xunit;

namespace CleanArchitectureMigration.Tests;

public class SafeArchiveExtractorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SafeArchiveExtractor _extractor = new();

    public SafeArchiveExtractorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "safe_extract_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task ExtractSafelyAsync_ValidArchive_ExtractsSuccessfullyAndComputesHash()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("src/Program.cs");
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes("Console.WriteLine(\"Hello Clean Arch\");");
            await entryStream.WriteAsync(bytes);
        }
        memoryStream.Position = 0;

        var extractDir = Path.Combine(_tempDir, "extracted");
        var result = await _extractor.ExtractSafelyAsync(memoryStream, extractDir);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Sha256);
        Assert.Equal(1, result.FileCount);
        Assert.True(File.Exists(Path.Combine(extractDir, "src", "Program.cs")));
    }

    [Fact]
    public async Task ExtractSafelyAsync_ZipSlipAttack_RejectsMaliciousPathTraversal()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("../../evil_escape.txt");
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes("Hacked!");
            await entryStream.WriteAsync(bytes);
        }
        memoryStream.Position = 0;

        var extractDir = Path.Combine(_tempDir, "extracted");
        var result = await _extractor.ExtractSafelyAsync(memoryStream, extractDir);

        Assert.False(result.Success);
        Assert.Contains("traversal", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(_tempDir, "evil_escape.txt")));
    }
}

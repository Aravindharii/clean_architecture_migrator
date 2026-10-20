using System.Text;
using System.Text.RegularExpressions;
using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.JavaWorker;

/// <summary>
/// Deterministic source-level refactoring for the layered-to-clean Java
/// migration. Rewrites package declarations (derived from the new directory
/// layout) and re-targets import statements through a package map. JPA/persistence
/// adapter inversion is intentionally flagged for manual work rather than
/// generated blindly, because it cannot be verified without a working JDK/Maven
/// toolchain.
/// </summary>
public static class JavaRefactoring
{
    private static readonly string[] IgnoredSegments = ["/target/", "\\target\\", "/build/", "\\build\\", "/.gradle/"];

    public static Dictionary<string, string> BuildPackageMap(MigrationPlan plan)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var op in plan.Operations.Where(o => o.Type == OperationType.RenameNamespaceOrPackage))
        {
            var oldPkg = op.Parameters.GetValueOrDefault("OldPackage",
                op.Parameters.GetValueOrDefault("OldNamespace", ""));
            var newPkg = op.Parameters.GetValueOrDefault("NewPackage",
                op.Parameters.GetValueOrDefault("NewNamespace", ""));
            if (string.IsNullOrWhiteSpace(newPkg) || string.IsNullOrWhiteSpace(oldPkg)) continue;
            map[oldPkg] = newPkg;
        }
        return map;
    }

    /// <summary>
    /// Rewrites every .java file in the working directory: the package
    /// declaration is aligned to the on-disk module layout and each import is
    /// mapped through the package map. Returns the list of changed files.
    /// </summary>
    public static async Task<List<string>> RewriteReferencesAsync(
        string workingDirectory,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        var packageMap = BuildPackageMap(plan);
        var changed = new List<string>();

        foreach (var file in EnumerateJavaFiles(workingDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var original = await File.ReadAllTextAsync(file, cancellationToken);
            var updated = RewriteFile(file, workingDirectory, original, packageMap);
            if (!string.Equals(original, updated, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(file, updated, cancellationToken);
                changed.Add(Path.GetRelativePath(workingDirectory, file));
            }
        }

        return changed;
    }

    public static string RewriteFile(
        string filePath,
        string workingDirectory,
        string source,
        IReadOnlyDictionary<string, string> packageMap)
    {
        var expectedPackage = DerivePackageFromPath(filePath, workingDirectory);

        // Align the package declaration with the module directory layout.
        source = Regex.Replace(source, @"(?m)^\s*package\s+[a-zA-Z0-9_\.]+\s*;",
            _ => string.IsNullOrEmpty(expectedPackage) ? string.Empty : $"package {expectedPackage};");

        // Rewrite imports. Handles normal, wildcard and static imports.
        source = Regex.Replace(source, @"(?m)^(\s*import\s+(?:static\s+)?)([a-zA-Z0-9_\.]+)(\s*;)$",
            m =>
            {
                var prefix = m.Groups[1].Value;
                var symbol = m.Groups[2].Value;

                var isWildcard = symbol.EndsWith(".*");
                var isStatic = prefix.Contains("static");

                string packagePart;
                string memberPart;

                if (isWildcard)
                {
                    packagePart = symbol[..^2];
                    memberPart = ".*";
                }
                else if (isStatic)
                {
                    var lastDot = symbol.LastIndexOf('.');
                    packagePart = lastDot > 0 ? symbol[..lastDot] : symbol;
                    memberPart = lastDot > 0 ? symbol[lastDot..] : string.Empty;
                }
                else
                {
                    var lastDot = symbol.LastIndexOf('.');
                    packagePart = lastDot > 0 ? symbol[..lastDot] : symbol;
                    memberPart = lastDot > 0 ? symbol[lastDot..] : string.Empty;
                }

                var mapped = MapPackage(packagePart, packageMap);
                return prefix + mapped + memberPart + m.Groups[3].Value;
            });

        return DeduplicateImports(source);
    }

    private static string MapPackage(string packageName, IReadOnlyDictionary<string, string> map)
    {
        if (map.TryGetValue(packageName, out var exact)) return exact;

        // Longest matching prefix (for sub-packages).
        var best = map.Keys
            .Where(k => packageName.StartsWith(k + ".", StringComparison.Ordinal))
            .OrderByDescending(k => k.Length)
            .FirstOrDefault();

        if (best != null)
        {
            return map[best] + packageName[best.Length..];
        }

        return packageName;
    }

    public static string DerivePackageFromPath(string javaFilePath, string workingDirectory)
    {
        var normalized = Path.GetRelativePath(workingDirectory, javaFilePath).Replace('\\', '/');
        var marker = "/src/main/java/";
        var index = normalized.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0) return string.Empty;

        var after = normalized[(index + marker.Length)..];
        var dir = after.Contains('/') ? after[..after.LastIndexOf('/')] : string.Empty;
        return dir.Replace('/', '.');
    }

    private static string DeduplicateImports(string source)
    {
        var lines = source.Replace("\r\n", "\n").Split('\n').ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("import ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal))
            {
                var key = string.Join(' ', trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                if (!seen.Add(key)) continue;
            }
            result.Add(line);
        }

        return string.Join('\n', result);
    }

    public static IEnumerable<string> EnumerateJavaFiles(string workingDirectory)
    {
        return Directory.EnumerateFiles(workingDirectory, "*.java", SearchOption.AllDirectories)
            .Where(f => !IgnoredSegments.Any(seg => f.Contains(seg, StringComparison.OrdinalIgnoreCase)));
    }
}

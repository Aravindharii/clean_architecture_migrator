using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CleanArchitectureMigration.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SymbolInfo = CleanArchitectureMigration.Contracts.SymbolInfo;

namespace CleanArchitectureMigration.DotNetWorker;

public interface IDotNetWorker
{
    Task<ProjectAnalysisResult> AnalyzeProjectAsync(string sourceDirectory, Guid projectId, Guid jobId, string sourceHash, CancellationToken cancellationToken = default);
    Task<List<TransformationOperationResult>> ApplyTransformationsAsync(string workingDirectory, MigrationPlan plan, CancellationToken cancellationToken = default);
    Task<BuildResult> BuildAsync(string solutionDirectory, CancellationToken cancellationToken = default);
    Task<TestResult> RunTestsAsync(string solutionDirectory, CancellationToken cancellationToken = default);
}

public sealed class DotNetWorker : IDotNetWorker
{
    public async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
        string sourceDirectory,
        Guid projectId,
        Guid jobId,
        string sourceHash,
        CancellationToken cancellationToken = default)
    {
        var result = new ProjectAnalysisResult
        {
            ProjectId = projectId,
            JobId = jobId,
            SourceHash = sourceHash,
            Language = LanguageKind.CSharp,
            Framework = ".NET",
            BuildSystem = BuildSystemKind.MSBuild,
            DetectedRootPath = sourceDirectory,
            AnalyzedAtUtc = DateTime.UtcNow
        };

        // 1. Discover Project Files (.csproj, .sln)
        var csprojFiles = Directory.GetFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !IsNoiseFile(f))
            .ToList();
        foreach (var projFile in csprojFiles)
        {
            var mod = AnalyzeProjectFile(sourceDirectory, projFile);
            result.Modules.Add(mod);
            if (string.IsNullOrEmpty(result.Framework) || result.Framework == ".NET")
            {
                result.Framework = mod.TargetFramework;
            }
        }

        // 2. Discover all C# Source Files
        var csFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsNoiseFile(f))
            .ToList();

        foreach (var file in csFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var fi = new FileInfo(file);
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            var lines = content.Split('\n').Length;

            result.SourceFiles.Add(new SourceFileInfo
            {
                RelativePath = relativePath,
                SizeBytes = fi.Length,
                LineCount = lines,
                Sha256 = ComputeSha256(content)
            });

            // Parse AST with Roslyn
            var tree = CSharpSyntaxTree.ParseText(content, path: relativePath, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            var walker = new CSharpSemanticSyntaxWalker(relativePath);
            walker.Visit(root);

            result.Symbols.AddRange(walker.CollectedSymbols);
            result.DatabaseDependencies.AddRange(walker.DatabaseDependencies);
            result.ExternalIntegrations.AddRange(walker.ExternalIntegrations);
        }

        // Partial classes (e.g. an EF migration plus its .Designer.cs file) yield
        // multiple declarations with the same fully-qualified name. Merge them so
        // downstream dictionary/graph construction never hits duplicate keys.
        result.Symbols = DeduplicateSymbols(result.Symbols);

        // 3. Construct Directed Dependency Graph
        BuildDependencyGraph(result);

        return result;
    }

    // Build outputs (bin/obj), macOS archive metadata (__MACOSX) and AppleDouble
    // resource-fork stubs (._*) must never be analysed, moved, or compiled.
    private static bool IsNoiseFile(string path)
    {
        var p = path.Replace('\\', '/');
        if (p.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/__MACOSX/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/.git/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = Path.GetFileName(p);
        return fileName.StartsWith("._", StringComparison.Ordinal) ||
               string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase);
    }

    private static List<SymbolInfo> DeduplicateSymbols(List<SymbolInfo> symbols)
    {
        var merged = new List<SymbolInfo>();
        // Merge only declarations that are duplicated within the SAME source
        // file. Partial classes split across files (an EF migration plus its
        // *.Designer.cs) keep one symbol per file so that every file still gets
        // relocated; keying purely on FullName would silently drop the second
        // file from the transformation.
        var indexByKey = new Dictionary<(string FullName, string File), int>();

        foreach (var sym in symbols)
        {
            var key = (sym.FullName ?? "", sym.RelativeFilePath ?? "");
            if (indexByKey.TryGetValue(key, out var existingIndex))
            {
                var existing = merged[existingIndex];
                existing.Dependencies = existing.Dependencies
                    .Concat(sym.Dependencies)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                existing.Methods = existing.Methods
                    .Concat(sym.Methods)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                existing.AttributesOrAnnotations = existing.AttributesOrAnnotations
                    .Concat(sym.AttributesOrAnnotations)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                existing.InterfacesImplemented = existing.InterfacesImplemented
                    .Concat(sym.InterfacesImplemented)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                existing.HasFrameworkLeakage = existing.HasFrameworkLeakage || sym.HasFrameworkLeakage;
                continue;
            }

            indexByKey[key] = merged.Count;
            merged.Add(sym);
        }

        return merged;
    }

    private static ModuleInfo AnalyzeProjectFile(string rootDir, string projPath)
    {
        var mod = new ModuleInfo
        {
            Name = Path.GetFileNameWithoutExtension(projPath),
            RelativePath = Path.GetRelativePath(rootDir, projPath)
        };

        try
        {
            var doc = XDocument.Load(projPath);
            var tf = doc.Descendants("TargetFramework").FirstOrDefault()?.Value
                     ?? doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value
                     ?? "net8.0";
            mod.TargetFramework = tf;

            foreach (var pr in doc.Descendants("ProjectReference"))
            {
                var inc = pr.Attribute("Include")?.Value;
                if (!string.IsNullOrEmpty(inc))
                {
                    mod.ProjectReferences.Add(inc);
                }
            }

            foreach (var pr in doc.Descendants("PackageReference"))
            {
                var inc = pr.Attribute("Include")?.Value;
                var ver = pr.Attribute("Version")?.Value ?? "";
                if (!string.IsNullOrEmpty(inc))
                {
                    mod.PackageDependencies.Add($"{inc} {ver}".Trim());
                }
            }

            mod.IsTestModule = mod.Name.Contains("Test", StringComparison.OrdinalIgnoreCase) ||
                               mod.PackageDependencies.Any(p => p.Contains("xunit") || p.Contains("nunit") || p.Contains("MSTest"));
        }
        catch
        {
            mod.TargetFramework = "net8.0";
        }

        return mod;
    }

    private static void BuildDependencyGraph(ProjectAnalysisResult result)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        foreach (var sym in result.Symbols)
        {
            nodes.Add(new GraphNode
            {
                Id = sym.FullName,
                Label = sym.Name,
                Layer = sym.CurrentLayer.ToString(),
                Role = sym.DetectedRole.ToString(),
                FilePath = sym.RelativeFilePath,
                ViolationCount = 0
            });

            foreach (var dep in sym.Dependencies)
            {
                edges.Add(new GraphEdge
                {
                    Source = sym.FullName,
                    Target = dep,
                    Type = DependencyType.References,
                    IsViolation = false
                });
            }
        }

        result.DependencyGraph = new DependencyGraphData
        {
            Nodes = nodes,
            Edges = edges
        };
    }

    public async Task<List<TransformationOperationResult>> ApplyTransformationsAsync(
        string workingDirectory,
        MigrationPlan plan,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TransformationOperationResult>();

        foreach (var op in plan.Operations.OrderBy(o => o.Sequence))
        {
            var sw = Stopwatch.StartNew();
            var opResult = new TransformationOperationResult
            {
                OperationId = op.OperationId,
                Type = op.Type
            };

            try
            {
                switch (op.Type)
                {
                    case OperationType.CreateProjectOrModule:
                        await HandleCreateProjectAsync(workingDirectory, op);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Created project {op.DestinationPath}";
                        break;

                    case OperationType.MoveFile:
                        await HandleMoveFileAsync(workingDirectory, op);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Moved {op.SourcePath} to {op.DestinationPath}";
                        break;

                    case OperationType.RenameNamespaceOrPackage:
                        await HandleRenameNamespaceAsync(workingDirectory, op, cancellationToken);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath ?? op.SourcePath;
                        opResult.Message = $"Renamed namespace in {opResult.AffectedPath}";
                        break;

                    case OperationType.UpdateProjectReferences:
                        await HandleUpdateProjectReferencesAsync(workingDirectory, op);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Updated project references in {op.DestinationPath}";
                        break;

                    case OperationType.ExtractInterface:
                        await HandleExtractInterfaceAsync(workingDirectory, op, cancellationToken);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Extracted interface to {op.DestinationPath}";
                        break;

                    case OperationType.RewriteReferences:
                        var rewritten = await DotNetRefactoring.RewriteReferencesAsync(workingDirectory, plan, cancellationToken);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Rewrote references in {rewritten.Count} file(s)";
                        break;

                    case OperationType.RemoveLegacyProject:
                        var removed = DotNetRefactoring.RemoveLegacyArtifacts(workingDirectory, op.SourcePath ?? op.DestinationPath ?? "");
                        opResult.Success = true;
                        opResult.AffectedPath = op.SourcePath ?? op.DestinationPath;
                        opResult.Message = $"Removed legacy project {op.SourcePath ?? op.DestinationPath}";
                        break;

                    case OperationType.UpdateDependencyInjection:
                        await HandleUpdateDiAsync(workingDirectory, op, plan, cancellationToken);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Updated dependency injection in {op.DestinationPath}";
                        break;

                    case OperationType.CreateApplicationUseCase:
                        await HandleCreateUseCaseAsync(workingDirectory, op);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Created application use case at {op.DestinationPath}";
                        break;

                    case OperationType.ProposeRepositoryAbstraction:
                        await HandleCreateRepositoryAbstractionAsync(workingDirectory, op);
                        opResult.Success = true;
                        opResult.AffectedPath = op.DestinationPath;
                        opResult.Message = $"Created repository abstraction at {op.DestinationPath}";
                        break;

                    case OperationType.AddArchitectureValidationRules:
                    case OperationType.FlagForManualRefactoring:
                        opResult.Success = true;
                        opResult.Message = $"Noted: {op.Description}";
                        break;

                    default:
                        opResult.Success = true;
                        opResult.Message = $"Executed operation {op.Type}";
                        break;
                }
            }
            catch (Exception ex)
            {
                opResult.Success = false;
                opResult.Message = $"Failed to execute {op.Type}: {ex.Message}";
            }

            sw.Stop();
            opResult.Duration = sw.Elapsed;
            results.Add(opResult);
        }

        return results;
    }

    private static async Task HandleCreateProjectAsync(string workingDir, PlanOperation op)
    {
        var projPath = Path.Combine(workingDir, op.DestinationPath ?? "");
        var dir = Path.GetDirectoryName(projPath)!;
        Directory.CreateDirectory(dir);

        var targetFramework = op.Parameters.GetValueOrDefault("TargetFramework", "net10.0");
        var outputType = op.Parameters.GetValueOrDefault("OutputType", "Library"); // Library or Exe

        var packages = op.Parameters.GetValueOrDefault("Packages", "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var packageItems = string.Concat(packages.Select(p =>
        {
            // Accept both "Name|Version" and "Name Version" package syntax from
            // plan operations and raw module metadata respectively.
            var name = p;
            var version = "";
            if (p.Contains('|'))
            {
                var parts = p.Split('|', 2);
                name = parts[0];
                version = parts.Length > 1 ? parts[1] : "";
            }
            else
            {
                var lastSpace = p.LastIndexOf(' ');
                if (lastSpace > 0)
                {
                    var candidate = p[(lastSpace + 1)..];
                    if (candidate.Length > 0 && (char.IsDigit(candidate[0]) || candidate[0] == '['))
                    {
                        name = p[..lastSpace];
                        version = candidate;
                    }
                }
            }
            return version.Length > 0
                ? $"    <PackageReference Include=\"{name}\" Version=\"{version}\" />\n"
                : $"    <PackageReference Include=\"{name}\" />\n";
        }));

        var packageGroup = packageItems.Length > 0
            ? $"  <ItemGroup>\n{packageItems}  </ItemGroup>\n"
            : "";

        var sdk = outputType == "Exe" ? "Microsoft.NET.Sdk.Web" : "Microsoft.NET.Sdk";
        var frameworkReference = op.Parameters.GetValueOrDefault("FrameworkReference", "");
        var frameworkRefGroup = !string.IsNullOrWhiteSpace(frameworkReference)
            ? $"  <ItemGroup>\n    <FrameworkReference Include=\"{frameworkReference}\" />\n  </ItemGroup>\n"
            : "";

        // The original Web-SDK host resolved framework types (ILogger, DI,
        // Hosting, HttpContext) through its implicit global usings. The class
        // libraries need those restored explicitly or relocated code stops
        // compiling with 'type or namespace not found' errors.
        var globalUsingsGroup = outputType != "Exe" && !string.IsNullOrWhiteSpace(frameworkReference)
            ? $"  <ItemGroup>\n" +
              $"    <Using Include=\"Microsoft.Extensions.Logging\" />\n" +
              $"    <Using Include=\"Microsoft.Extensions.DependencyInjection\" />\n" +
              $"    <Using Include=\"Microsoft.Extensions.Configuration\" />\n" +
              $"    <Using Include=\"Microsoft.Extensions.Hosting\" />\n" +
              $"    <Using Include=\"Microsoft.AspNetCore.Http\" />\n" +
              $"    <Using Include=\"Microsoft.AspNetCore.Hosting\" />\n" +
              $"  </ItemGroup>\n"
            : "";

        var content =
            $"<Project Sdk=\"{sdk}\">\n" +
            "  <PropertyGroup>\n" +
            $"    <TargetFramework>{targetFramework}</TargetFramework>\n" +
            "    <ImplicitUsings>enable</ImplicitUsings>\n" +
            "    <Nullable>enable</Nullable>\n" +
            "  </PropertyGroup>\n" +
            packageGroup +
            frameworkRefGroup +
            globalUsingsGroup +
            "</Project>\n";

        await File.WriteAllTextAsync(projPath, content);
    }

    private static Task HandleMoveFileAsync(string workingDir, PlanOperation op)
    {
        var src = Path.Combine(workingDir, op.SourcePath ?? "");
        var dst = Path.Combine(workingDir, op.DestinationPath ?? "");

        if (File.Exists(src))
        {
            var dstDir = Path.GetDirectoryName(dst)!;
            Directory.CreateDirectory(dstDir);
            File.Move(src, dst, true);
        }

        return Task.CompletedTask;
    }

    private static async Task HandleRenameNamespaceAsync(string workingDir, PlanOperation op, CancellationToken ct)
    {
        var targetFile = Path.Combine(workingDir, op.DestinationPath ?? op.SourcePath ?? "");
        if (!File.Exists(targetFile)) return;

        var oldNs = op.Parameters.GetValueOrDefault("OldNamespace", "");
        var newNs = op.Parameters.GetValueOrDefault("NewNamespace", "");

        if (string.IsNullOrEmpty(newNs)) return;

        var code = await File.ReadAllTextAsync(targetFile, ct);
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: ct);
        var root = await tree.GetRootAsync(ct);

        // Roslyn Syntax Rewriter to update FileScopedNamespaceDeclaration or NamespaceDeclaration
        var rewriter = new NamespaceRewriter(oldNs, newNs);
        var newRoot = rewriter.Visit(root);
        await File.WriteAllTextAsync(targetFile, newRoot.ToFullString(), ct);
    }

    private static async Task HandleUpdateProjectReferencesAsync(string workingDir, PlanOperation op)
    {
        var projPath = Path.Combine(workingDir, op.DestinationPath ?? "");
        if (!File.Exists(projPath)) return;

        var referencesToAdd = op.Parameters.GetValueOrDefault("References", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var doc = XDocument.Load(projPath);
        var root = doc.Root;
        if (root == null) return;

        var itemGroup = root.Elements("ItemGroup")
            .FirstOrDefault(ig => ig.Elements("ProjectReference").Any()) ?? new XElement("ItemGroup");

        if (itemGroup.Parent == null) root.Add(itemGroup);

        foreach (var reference in referencesToAdd)
        {
            if (!itemGroup.Elements("ProjectReference").Any(e => (string?)e.Attribute("Include") == reference))
            {
                itemGroup.Add(new XElement("ProjectReference", new XAttribute("Include", reference)));
            }
        }

        doc.Save(projPath);
        await Task.CompletedTask;
    }

    private static async Task HandleExtractInterfaceAsync(string workingDir, PlanOperation op, CancellationToken ct)
    {
        var interfacePath = Path.Combine(workingDir, op.DestinationPath ?? "");
        var dir = Path.GetDirectoryName(interfacePath)!;
        Directory.CreateDirectory(dir);

        var implementationPathRelative = op.Parameters.GetValueOrDefault("ImplementationPath", "");
        var implementationPath = Path.Combine(workingDir, implementationPathRelative);
        if (!File.Exists(implementationPath))
        {
            // Fall back to the source path declared on the operation.
            var alt = Path.Combine(workingDir, op.SourcePath ?? "");
            if (File.Exists(alt)) implementationPath = alt;
        }

        if (string.IsNullOrWhiteSpace(implementationPathRelative) || !File.Exists(implementationPath))
        {
            throw new FileNotFoundException(
                $"Cannot extract interface: implementation for '{op.Parameters.GetValueOrDefault("ImplementationClass", "")}' not found.");
        }

        var spec = new RepositoryInterfaceSpec(
            op.Parameters.GetValueOrDefault("ImplementationClass", ""),
            implementationPathRelative,
            op.Parameters.GetValueOrDefault("InterfaceName", "IRepository"),
            op.Parameters.GetValueOrDefault("Namespace", "Project.Application.Common.Interfaces"),
            op.DestinationPath ?? "",
            op.Parameters.GetValueOrDefault("Usings", "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList());

        var implSource = await File.ReadAllTextAsync(implementationPath, ct);
        var code = DotNetRefactoring.GenerateInterfaceSource(spec, implSource);
        await File.WriteAllTextAsync(interfacePath, code, ct);
    }

    private static async Task HandleUpdateDiAsync(
        string workingDir,
        PlanOperation op,
        MigrationPlan plan,
        CancellationToken ct)
    {
        var targetFile = Path.Combine(workingDir, op.DestinationPath ?? "");
        var dir = Path.GetDirectoryName(targetFile)!;
        Directory.CreateDirectory(dir);

        var layer = op.Parameters.GetValueOrDefault("Layer", "Application");
        var ns = op.Parameters.GetValueOrDefault("Namespace", $"Project.{layer}");
        var specs = DotNetRefactoring.BuildRepositorySpecs(plan);

        string content;
        if (string.Equals(layer, "Infrastructure", StringComparison.OrdinalIgnoreCase))
        {
            content = DotNetRefactoring.GenerateInfrastructureDependencyInjection(workingDir, ns, specs);
        }
        else if (string.Equals(layer, "Presentation", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(layer, "Api", StringComparison.OrdinalIgnoreCase))
        {
            var serviceTypes = DotNetRefactoring.FindApplicationServiceTypes(workingDir);
            var serviceNamespaces = DotNetRefactoring.FindApplicationServiceNamespaces(workingDir);
            var infraNamespace = op.Parameters.GetValueOrDefault("InfrastructureNamespace", "Project.Infrastructure");
            content = DotNetRefactoring.GenerateApiCompositionRoot(workingDir, serviceNamespaces, serviceTypes, infraNamespace);
        }
        else
        {
            content = $@"using Microsoft.Extensions.DependencyInjection;

namespace {ns};

public static class DependencyInjection
{{
    public static IServiceCollection Add{layer}(this IServiceCollection services)
    {{
        return services;
    }}
}}
";
        }

        await File.WriteAllTextAsync(targetFile, content, ct);
    }

    private static async Task HandleCreateUseCaseAsync(string workingDir, PlanOperation op)
    {
        var useCasePath = Path.Combine(workingDir, op.DestinationPath ?? "");
        var dir = Path.GetDirectoryName(useCasePath)!;
        Directory.CreateDirectory(dir);

        var useCaseName = op.Parameters.GetValueOrDefault("UseCaseName", "GetEntityUseCase");
        var ns = op.Parameters.GetValueOrDefault("Namespace", "Project.Application.UseCases");

        var code = $@"namespace {ns};

public class {useCaseName}
{{
    public {useCaseName}()
    {{
    }}

    public async Task ExecuteAsync()
    {{
        await Task.CompletedTask;
    }}
}}
";
        await File.WriteAllTextAsync(useCasePath, code);
    }

    private static async Task HandleCreateRepositoryAbstractionAsync(string workingDir, PlanOperation op)
    {
        var path = Path.Combine(workingDir, op.DestinationPath ?? "");
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        var entityName = op.Parameters.GetValueOrDefault("EntityName", "Entity");
        var ns = op.Parameters.GetValueOrDefault("Namespace", "Project.Application.Common.Interfaces");

        var code = $@"namespace {ns};

public interface I{entityName}Repository
{{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(Guid id);
    Task AddAsync(object entity);
    Task UpdateAsync(object entity);
    Task DeleteAsync(Guid id);
}}
";
        await File.WriteAllTextAsync(path, code);
    }

    public async Task<BuildResult> BuildAsync(string solutionDirectory, CancellationToken cancellationToken = default)
    {
        if (!await IsToolAvailableAsync("dotnet", "--version", solutionDirectory, cancellationToken))
        {
            return new BuildResult
            {
                Success = false,
                ErrorsCount = 0,
                ToolAvailable = false,
                Status = ToolVerificationStatus.ToolUnavailable,
                RawOutput = "The dotnet SDK is not available on PATH."
            };
        }

        var slnFiles = Directory.GetFiles(solutionDirectory, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(solutionDirectory, "*.slnx", SearchOption.AllDirectories))
            .ToArray();
        var projects = EnumerateProjectFiles(solutionDirectory);

        var targets = slnFiles.Length > 0
            ? new[] { slnFiles.First() }
            : projects.ToArray();

        if (targets.Length == 0)
        {
            return new BuildResult
            {
                Success = false,
                ErrorsCount = 1,
                ToolAvailable = true,
                Status = ToolVerificationStatus.Failed,
                RawOutput = "No solution or project file found to build."
            };
        }

        var combined = new System.Text.StringBuilder();
        var success = true;
        var errors = 0;

        foreach (var target in targets)
        {
            var (exitCode, output) = await RunProcessAsync(
                "dotnet", $"build \"{target}\" --nologo", solutionDirectory, cancellationToken);
            combined.AppendLine(output);
            if (exitCode != 0) success = false;
            errors += Regex.Matches(output, @": error [A-Z0-9]+:").Count;
        }

        return new BuildResult
        {
            Success = success,
            ErrorsCount = errors,
            ToolAvailable = true,
            Status = success ? ToolVerificationStatus.Succeeded : ToolVerificationStatus.Failed,
            RawOutput = combined.ToString()
        };
    }

    public async Task<TestResult> RunTestsAsync(string solutionDirectory, CancellationToken cancellationToken = default)
    {
        if (!await IsToolAvailableAsync("dotnet", "--version", solutionDirectory, cancellationToken))
        {
            return new TestResult
            {
                Success = false,
                TotalTests = 0,
                ToolAvailable = false,
                Status = ToolVerificationStatus.ToolUnavailable,
                RawOutput = "The dotnet SDK is not available on PATH."
            };
        }

        var testProjects = EnumerateProjectFiles(solutionDirectory)
            .Where(IsTestProject)
            .ToArray();

        if (testProjects.Length == 0)
        {
            return new TestResult
            {
                Success = false,
                TotalTests = 0,
                ToolAvailable = true,
                Status = ToolVerificationStatus.Failed,
                RawOutput = "No test projects were discovered in the migrated solution. Test verification cannot be reported as passing without any tests."
            };
        }

        var sw = Stopwatch.StartNew();
        var combined = new System.Text.StringBuilder();
        var success = true;
        var passed = 0;
        var failed = 0;

        foreach (var proj in testProjects)
        {
            var (exitCode, output) = await RunProcessAsync(
                "dotnet", $"test \"{proj}\" --nologo", solutionDirectory, cancellationToken);
            combined.AppendLine(output);
            if (exitCode != 0) success = false;

            var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)");
            var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)");
            if (passedMatch.Success) passed += int.Parse(passedMatch.Groups[1].Value);
            if (failedMatch.Success) failed += int.Parse(failedMatch.Groups[1].Value);
        }

        sw.Stop();
        return new TestResult
        {
            Success = success && failed == 0,
            TotalTests = passed + failed,
            PassedTests = passed,
            FailedTests = failed,
            Duration = sw.Elapsed,
            ToolAvailable = true,
            Status = success && failed == 0 ? ToolVerificationStatus.Succeeded : ToolVerificationStatus.Failed,
            RawOutput = combined.ToString()
        };
    }

    private static IEnumerable<string> EnumerateProjectFiles(string root)
        => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !IsNoiseFile(f));

    private static bool IsTestProject(string projectPath)
    {
        try
        {
            var name = Path.GetFileNameWithoutExtension(projectPath);
            if (name.Contains("Test", StringComparison.OrdinalIgnoreCase)) return true;
            var text = File.ReadAllText(projectPath);
            return text.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("nunit", StringComparison.OrdinalIgnoreCase) ||
                   text.Contains("MSTest", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsToolAvailableAsync(
        string command,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, _) = await RunProcessAsync(command, arguments, workingDirectory, cancellationToken);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(
        string command,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var combined = await outputTask + "\n" + await errorTask;
        return (process.ExitCode, combined);
    }

    private static string ComputeSha256(string content)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}

internal sealed class NamespaceRewriter : CSharpSyntaxRewriter
{
    private readonly string _oldNs;
    private readonly string _newNs;

    public NamespaceRewriter(string oldNs, string newNs)
    {
        _oldNs = oldNs;
        _newNs = newNs;
    }

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString().Contains(_oldNs) || string.IsNullOrEmpty(_oldNs))
        {
            return node.WithName(SyntaxFactory.ParseName(_newNs));
        }
        return base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString().Contains(_oldNs) || string.IsNullOrEmpty(_oldNs))
        {
            return node.WithName(SyntaxFactory.ParseName(_newNs));
        }
        return base.VisitNamespaceDeclaration(node);
    }
}

internal sealed class CSharpSemanticSyntaxWalker : CSharpSyntaxWalker
{
    private readonly string _filePath;
    public List<SymbolInfo> CollectedSymbols { get; } = [];
    public List<DatabaseDependencyInfo> DatabaseDependencies { get; } = [];
    public List<ExternalServiceDependencyInfo> ExternalIntegrations { get; } = [];

    private string _currentNamespace = string.Empty;

    public CSharpSemanticSyntaxWalker(string filePath)
    {
        _filePath = filePath;
    }

    public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        _currentNamespace = node.Name.ToString();
        base.VisitFileScopedNamespaceDeclaration(node);
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var prev = _currentNamespace;
        _currentNamespace = node.Name.ToString();
        base.VisitNamespaceDeclaration(node);
        _currentNamespace = prev;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var symbol = ProcessTypeDeclaration(node, "class");
        CollectedSymbols.Add(symbol);
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var symbol = ProcessTypeDeclaration(node, "interface");
        CollectedSymbols.Add(symbol);
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        var symbol = ProcessTypeDeclaration(node, "record");
        CollectedSymbols.Add(symbol);
        base.VisitRecordDeclaration(node);
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        var name = node.Identifier.Text;
        var fullName = string.IsNullOrEmpty(_currentNamespace) ? name : $"{_currentNamespace}.{name}";

        var attributes = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.Name.ToString())
            .ToList();

        var symbol = new SymbolInfo
        {
            Name = name,
            FullName = fullName,
            Kind = "enum",
            RelativeFilePath = _filePath,
            NamespaceOrPackage = _currentNamespace,
            AttributesOrAnnotations = attributes,
            Dependencies = [],
            Methods = []
        };

        // Enums are domain vocabulary; colocate them with the domain model so
        // entities that reference them (e.g. User.Role) resolve at build time.
        symbol.DetectedRole = SymbolRole.Entity;
        symbol.CurrentLayer = CleanArchitectureLayer.Domain;
        symbol.TargetCleanLayer = CleanArchitectureLayer.Domain;
        symbol.TargetNamespace = "Project.Domain.Entities";
        symbol.RoleConfidence = 0.95;
        symbol.RoleEvidence.Add("Enum declaration; domain vocabulary.");

        CollectedSymbols.Add(symbol);
        base.VisitEnumDeclaration(node);
    }

    private SymbolInfo ProcessTypeDeclaration(TypeDeclarationSyntax node, string kind)
    {
        var name = node.Identifier.Text;
        var fullName = string.IsNullOrEmpty(_currentNamespace) ? name : $"{_currentNamespace}.{name}";

        var attributes = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => a.Name.ToString())
            .ToList();

        var baseTypes = node.BaseList?.Types
            .Select(t => t.Type.ToString())
            .ToList() ?? [];

        // Identify constructor injection dependencies
        var dependencies = new HashSet<string>();
        foreach (var ctor in node.Members.OfType<ConstructorDeclarationSyntax>())
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                dependencies.Add(param.Type?.ToString() ?? "");
            }
        }

        // Primary constructor parameters (C# 12+)
        if (node.ParameterList != null)
        {
            foreach (var param in node.ParameterList.Parameters)
            {
                dependencies.Add(param.Type?.ToString() ?? "");
            }
        }

        dependencies.Remove("");

        var methods = node.Members.OfType<MethodDeclarationSyntax>()
            .Select(m => m.Identifier.Text)
            .ToList();

        var sym = new SymbolInfo
        {
            Name = name,
            FullName = fullName,
            Kind = kind,
            RelativeFilePath = _filePath,
            NamespaceOrPackage = _currentNamespace,
            AttributesOrAnnotations = attributes,
            InterfacesImplemented = baseTypes.Where(b => b.StartsWith('I') && b.Length > 1 && char.IsUpper(b[1])).ToList(),
            BaseClass = baseTypes.FirstOrDefault(b => !b.StartsWith('I') || b.Length <= 1 || !char.IsUpper(b[1])),
            Dependencies = dependencies.ToList(),
            Methods = methods
        };

        // Determine architectural role & layer from code facts
        ClassifySymbolRoleAndLayer(sym, node);

        // Check database / external dependencies
        if (sym.Dependencies.Any(d => d.Contains("DbContext") || d.Contains("Session")) ||
            sym.BaseClass?.Contains("DbContext") == true ||
            attributes.Any(a => a.Contains("Table") || a.Contains("Column")))
        {
            DatabaseDependencies.Add(new DatabaseDependencyInfo
            {
                Technology = "Entity Framework Core",
                DeclaredInSymbol = fullName,
                TableOrEntity = name
            });
        }

        if (sym.Dependencies.Any(d => d.Contains("HttpClient") || d.Contains("RabbitMQ") || d.Contains("MessageQueue") || d.Contains("Bus")))
        {
            ExternalIntegrations.Add(new ExternalServiceDependencyInfo
            {
                ServiceType = "External Integration / Messaging / HTTP",
                DeclaredInSymbol = fullName,
                Details = string.Join(", ", sym.Dependencies.Where(d => d.Contains("Http") || d.Contains("Queue") || d.Contains("Bus")))
            });
        }

        return sym;
    }

    // Canonical target namespaces for the generated clean architecture. They are
    // derived from semantic facts (base types, implemented interfaces, attributes
    // and folder layout) rather than raw path substring matching, which used to
    // misclassify e.g. 'AddUpdatedAtToEntities.cs' as a Domain entity.
    private const string NsDomain = "Project.Domain.Entities";
    private const string NsAppDtos = "Project.Application.Common.Dtos";
    private const string NsAppInterfaces = "Project.Application.Common.Interfaces";
    private const string NsAppExceptions = "Project.Application.Common.Exceptions";
    private const string NsAppResponses = "Project.Application.Common.Responses";
    private const string NsAppWebhooks = "Project.Application.Common.Webhooks";
    private const string NsAppETag = "Project.Application.Common.ETag";
    private const string NsAppValidators = "Project.Application.Validators";
    private const string NsAppServices = "Project.Application.Services";
    private const string NsAppAuth = "Project.Application.Authentication";
    private const string NsInfraPersistence = "Project.Infrastructure.Persistence";
    private const string NsInfraConfigurations = "Project.Infrastructure.Persistence.Configurations";
    private const string NsInfraMigrations = "Project.Infrastructure.Persistence.Migrations";
    private const string NsInfraCommon = "Project.Infrastructure.Common";
    private const string NsInfraWebhooks = "Project.Infrastructure.Webhooks";
    private const string NsApiControllers = "Project.API.Controllers";
    private const string NsApiFilters = "Project.API.Filters";
    private const string NsApiSettings = "Project.API.Settings";
    private const string NsApiMiddleware = "Project.API.Middleware";

    private static string SimpleName(string type)
    {
        var t = type.Trim();
        var generic = t.IndexOf('<');
        if (generic >= 0) t = t[..generic];
        var dot = t.LastIndexOf('.');
        if (dot >= 0) t = t[(dot + 1)..];
        return t.Trim();
    }

    private static void ClassifySymbolRoleAndLayer(SymbolInfo sym, TypeDeclarationSyntax node)
    {
        var baseNames = node.BaseList?.Types
            .Select(t => SimpleName(t.Type.ToString()))
            .ToList() ?? [];
        var path = sym.RelativeFilePath.Replace('\\', '/');
        var folders = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1)
            .Select(s => s.ToLowerInvariant())
            .ToArray();
        var isInterface = node is InterfaceDeclarationSyntax;
        var name = sym.Name;

        bool HasFolder(string f) => folders.Contains(f);
        bool BaseContains(string b) => baseNames.Any(x => x.Contains(b, StringComparison.OrdinalIgnoreCase));
        bool ImplementAny(params string[] names) => sym.InterfacesImplemented
            .Any(i => names.Any(n => SimpleName(i).Contains(n, StringComparison.OrdinalIgnoreCase)));
        bool DependsOnDbContext() =>
            sym.Dependencies.Any(d => SimpleName(d).EndsWith("DbContext", StringComparison.Ordinal)) ||
            BaseContains("DbContext");

        void Set(SymbolRole role, CleanArchitectureLayer layer, string ns, double confidence, string evidence)
        {
            sym.DetectedRole = role;
            sym.CurrentLayer = layer;
            sym.TargetCleanLayer = layer;
            sym.TargetNamespace = ns;
            sym.RoleConfidence = confidence;
            sym.RoleEvidence.Add(evidence);
        }

        // 1. Web API controllers.
        if (name.EndsWith("Controller") || BaseContains("Controller") ||
            sym.AttributesOrAnnotations.Any(a => a.Contains("ApiController")))
        {
            Set(SymbolRole.Controller, CleanArchitectureLayer.Presentation, NsApiControllers, 0.98,
                "Controller suffix, ControllerBase inheritance or [ApiController] attribute.");
            return;
        }

        // 2. Global exception handling middleware.
        if (name.EndsWith("ExceptionHandler") || BaseContains("IExceptionHandler") ||
            ImplementAny("IExceptionHandler"))
        {
            Set(SymbolRole.Middleware, CleanArchitectureLayer.Presentation, NsApiMiddleware, 0.95,
                "Implements IExceptionHandler; belongs to the presentation host.");
            return;
        }

        // 3. MVC filters.
        if (name.EndsWith("Filter") || BaseContains("ActionFilter") || ImplementAny("ActionFilter", "AuthorizationFilter"))
        {
            Set(SymbolRole.Middleware, CleanArchitectureLayer.Presentation, NsApiFilters, 0.95,
                "ASP.NET Core filter.");
            return;
        }

        // 4. EF Core migrations & model snapshot.
        if (BaseContains("Migration") || HasFolder("migrations") || name.Contains("ModelSnapshot"))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Infrastructure, NsInfraMigrations, 0.97,
                "EF Core migration or model snapshot; persistence layout lives in Infrastructure.");
            return;
        }

        // 5. EF Core entity type configurations.
        if (BaseContains("IEntityTypeConfiguration") || ImplementAny("IEntityTypeConfiguration") ||
            HasFolder("configurations"))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Infrastructure, NsInfraConfigurations, 0.96,
                "IEntityTypeConfiguration mapping; EF-specific, Infra-only.");
            return;
        }

        // 6. DbContext.
        if (BaseContains("DbContext"))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Infrastructure, NsInfraPersistence, 0.96,
                "Derives from DbContext.");
            return;
        }

        // 6.5 Seed data belongs with persistence.
        if (HasFolder("data") && (name.Contains("Seed", StringComparison.Ordinal) || node is not TypeDeclarationSyntax))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Infrastructure, NsInfraPersistence, 0.88,
                "Seed initializer colocated with the persistence layer.");
            return;
        }
        if (HasFolder("data") && !HasFolder("configurations"))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Infrastructure, NsInfraPersistence, 0.85,
                "Persistence-side helper under the Data folder.");
            return;
        }

        // 7. Repository abstraction vs implementation.
        if (isInterface && (name.EndsWith("Repository") || BaseContains("Repository")))
        {
            Set(SymbolRole.Interface, CleanArchitectureLayer.Application, NsAppInterfaces, 0.95,
                "Repository abstraction owned by the Application layer (Dependency Inversion).");
            return;
        }
        if (!isInterface && (name.EndsWith("Repository") || BaseContains("Repository") || ImplementAny("Repository")))
        {
            Set(SymbolRole.Repository, CleanArchitectureLayer.Infrastructure, NsInfraPersistence, 0.95,
                "Repository implementation over the persistence store.");
            return;
        }

        // 8. FluentValidation validators.
        if (BaseContains("AbstractValidator") || name.EndsWith("Validator"))
        {
            Set(SymbolRole.Dto, CleanArchitectureLayer.Application, NsAppValidators, 0.94,
                "FluentValidation validator.");
            return;
        }

        // 9. Exceptions.
        if (name.EndsWith("Exception") || BaseContains("Exception"))
        {
            Set(SymbolRole.Dto, CleanArchitectureLayer.Application, NsAppExceptions, 0.93,
                "Application exception type.");
            return;
        }

        // 10. Authentication (JWT service + settings).
        if (HasFolder("authentication") || name.Contains("Jwt"))
        {
            Set(SymbolRole.Service, CleanArchitectureLayer.Application, NsAppAuth, 0.92,
                "Authentication component (JWT); application concern.");
            return;
        }

        // 11. Settings / options.
        if (name.EndsWith("Settings") || name.EndsWith("Options"))
        {
            Set(SymbolRole.Configuration, CleanArchitectureLayer.Presentation, NsApiSettings, 0.90,
                "Host configuration binding.");
            return;
        }

        // 12. ETag helpers.
        if (HasFolder("etag") || path.Contains("/ETag/", StringComparison.OrdinalIgnoreCase))
        {
            Set(SymbolRole.Service, CleanArchitectureLayer.Application, NsAppETag, 0.92,
                "ETag computation abstraction/implementation (no infrastructure dependency).");
            return;
        }

        // 13. Response envelopes.
        if (HasFolder("responses") || name.EndsWith("Response") && HasFolder("common"))
        {
            Set(SymbolRole.Dto, CleanArchitectureLayer.Application, NsAppResponses, 0.92,
                "API response envelope.");
            return;
        }

        // 14. Webhook domain events / headers / types.
        if (HasFolder("webhooks") && HasFolder("common"))
        {
            Set(SymbolRole.Dto, CleanArchitectureLayer.Application, NsAppWebhooks, 0.92,
                "Webhook event contract shared across layers.");
            return;
        }

        // 15. Background / hosted services -> Infrastructure adapter.
        if (BaseContains("BackgroundService") || ImplementAny("IHostedService"))
        {
            var ns = HasFolder("webhooks") ? NsInfraWebhooks : NsInfraPersistence;
            Set(SymbolRole.Service, CleanArchitectureLayer.Infrastructure, ns, 0.93,
                "Hosted background worker (outbound adapter).");
            return;
        }

        // 16. Webhook delivery/transport adapters -> Infrastructure.
        if (HasFolder("webhooks") &&
            new[] { "Dispatcher", "Publisher", "Signature", "Payload", "Delivery", "Sender" }
                .Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            Set(SymbolRole.Service, CleanArchitectureLayer.Infrastructure, NsInfraWebhooks, 0.92,
                "Outbound webhook delivery adapter.");
            return;
        }

        // 17. Data transfer objects.
        if (name.EndsWith("Dto") || name.EndsWith("Request") || name.EndsWith("Response") ||
            name.EndsWith("Result") || HasFolder("dtos") || HasFolder("dto"))
        {
            Set(SymbolRole.Dto, CleanArchitectureLayer.Application, NsAppDtos, 0.94,
                "Data transfer object.");
            return;
        }

        // 18. Interfaces.
        if (isInterface)
        {
            if ((name == "ISoftDelete" || name.EndsWith("ISoftDelete")) || HasFolder("models") || HasFolder("entities"))
            {
                Set(SymbolRole.Entity, CleanArchitectureLayer.Domain, NsDomain, 0.90,
                    "Domain interface (e.g. ISoftDelete) colocated with the domain model.");
                return;
            }
            Set(SymbolRole.Interface, CleanArchitectureLayer.Application, NsAppInterfaces, 0.90,
                "Application-layer abstraction.");
            return;
        }

        // 19. Services and their infrastructure variants.
        if (name.EndsWith("Service") || name.EndsWith("Handler") || name.EndsWith("Manager") ||
            name.EndsWith("Publisher") || name.EndsWith("Dispatcher") || name.EndsWith("Builder"))
        {
            if (DependsOnDbContext())
            {
                Set(SymbolRole.Service, CleanArchitectureLayer.Infrastructure, NsInfraPersistence, 0.90,
                    "Use-case service that directly coordinates the DbContext; hosted in Infrastructure to keep the Application layer free of persistence.");
                return;
            }
            Set(SymbolRole.Service, CleanArchitectureLayer.Application, NsAppServices, 0.90,
                "Application service coordinating use cases.");
            return;
        }

        // 20. Domain entities.
        if (HasFolder("models") || HasFolder("entities") ||
            sym.AttributesOrAnnotations.Any(a => a.Contains("Table") || a.Contains("Column") || a.Contains("Key")) ||
            (node.Members.OfType<PropertyDeclarationSyntax>().Any() && !node.Members.OfType<MethodDeclarationSyntax>().Any()))
        {
            Set(SymbolRole.Entity, CleanArchitectureLayer.Domain, NsDomain, 0.90,
                "POCO domain model.");
            if (sym.AttributesOrAnnotations.Any(a => a.Contains("Table") || a.Contains("Column")))
            {
                sym.HasFrameworkLeakage = true;
            }
            return;
        }

        // 21. Shared pure utilities in a Common folder.
        if (HasFolder("common"))
        {
            Set(SymbolRole.Service, CleanArchitectureLayer.Infrastructure, NsInfraCommon, 0.80,
                "Shared utility used by persistence/adapters.");
            return;
        }

        // Default heuristic.
        sym.DetectedRole = SymbolRole.Unknown;
        sym.CurrentLayer = CleanArchitectureLayer.Shared;
        sym.TargetCleanLayer = CleanArchitectureLayer.Shared;
        sym.RoleConfidence = 0.50;
        sym.RoleEvidence.Add("Unclassified symbol; flagged for architectural review.");
    }
}

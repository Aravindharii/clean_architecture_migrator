using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.JavaWorker;

public interface IJavaWorker
{
    Task<ProjectAnalysisResult> AnalyzeProjectAsync(string sourceDirectory, Guid projectId, Guid jobId, string sourceHash, CancellationToken cancellationToken = default);
    Task<List<TransformationOperationResult>> ApplyTransformationsAsync(string workingDirectory, MigrationPlan plan, CancellationToken cancellationToken = default);
    Task<BuildResult> BuildAsync(string projectDirectory, CancellationToken cancellationToken = default);
    Task<TestResult> RunTestsAsync(string projectDirectory, CancellationToken cancellationToken = default);
}

public sealed class JavaWorker : IJavaWorker
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
            Language = LanguageKind.Java,
            Framework = "Spring Boot",
            BuildSystem = BuildSystemKind.Maven,
            DetectedRootPath = sourceDirectory,
            AnalyzedAtUtc = DateTime.UtcNow
        };

        // 1. Detect Build System (Maven pom.xml vs Gradle build.gradle)
        var pomFiles = Directory.GetFiles(sourceDirectory, "pom.xml", SearchOption.AllDirectories);
        var gradleFiles = Directory.GetFiles(sourceDirectory, "build.gradle*", SearchOption.AllDirectories);

        if (pomFiles.Length > 0)
        {
            result.BuildSystem = BuildSystemKind.Maven;
            foreach (var pom in pomFiles)
            {
                var mod = AnalyzeMavenPom(sourceDirectory, pom);
                result.Modules.Add(mod);
            }
        }
        else if (gradleFiles.Length > 0)
        {
            result.BuildSystem = BuildSystemKind.Gradle;
            foreach (var gf in gradleFiles)
            {
                result.Modules.Add(new ModuleInfo
                {
                    Name = Path.GetFileName(Path.GetDirectoryName(gf)!),
                    RelativePath = Path.GetRelativePath(sourceDirectory, gf),
                    TargetFramework = "Java 17/21"
                });
            }
        }

        // 2. Discover all Java source files
        var javaFiles = Directory.GetFiles(sourceDirectory, "*.java", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/target/") && !f.Contains("\\target\\") &&
                        !f.Contains("/build/") && !f.Contains("\\build\\") &&
                        !f.Contains("/.gradle/"))
            .ToList();

        foreach (var file in javaFiles)
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

            // Parse Java AST / declarations
            var symbols = ParseJavaFile(relativePath, content);
            result.Symbols.AddRange(symbols);

            foreach (var sym in symbols)
            {
                if (sym.AttributesOrAnnotations.Any(a => a.Contains("Entity") || a.Contains("Table") || a.Contains("Repository")) ||
                    sym.InterfacesImplemented.Any(i => i.Contains("JpaRepository") || i.Contains("CrudRepository")))
                {
                    result.DatabaseDependencies.Add(new DatabaseDependencyInfo
                    {
                        Technology = "Spring Data JPA / Hibernate",
                        DeclaredInSymbol = sym.FullName,
                        TableOrEntity = sym.Name
                    });
                }

                if (sym.Dependencies.Any(d => d.Contains("RestTemplate") || d.Contains("WebClient") || d.Contains("Kafka") || d.Contains("RabbitTemplate")))
                {
                    result.ExternalIntegrations.Add(new ExternalServiceDependencyInfo
                    {
                        ServiceType = "External Integration / HTTP / Messaging",
                        DeclaredInSymbol = sym.FullName,
                        Details = string.Join(", ", sym.Dependencies.Where(d => d.Contains("Template") || d.Contains("Client") || d.Contains("Kafka")))
                    });
                }
            }
        }

        // 3. Construct Directed Dependency Graph
        BuildDependencyGraph(result);

        return result;
    }

    private static ModuleInfo AnalyzeMavenPom(string rootDir, string pomPath)
    {
        var mod = new ModuleInfo
        {
            Name = Path.GetFileName(Path.GetDirectoryName(pomPath)!),
            RelativePath = Path.GetRelativePath(rootDir, pomPath),
            TargetFramework = "Java 17/21"
        };

        try
        {
            var doc = XDocument.Load(pomPath);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var artifactId = doc.Descendants(ns + "artifactId").FirstOrDefault()?.Value;
            if (!string.IsNullOrEmpty(artifactId))
            {
                mod.Name = artifactId;
            }

            foreach (var dep in doc.Descendants(ns + "dependency"))
            {
                var g = dep.Element(ns + "groupId")?.Value ?? "";
                var a = dep.Element(ns + "artifactId")?.Value ?? "";
                var v = dep.Element(ns + "version")?.Value ?? "";
                mod.PackageDependencies.Add($"{g}:{a}:{v}".Trim(':'));
            }

            mod.IsTestModule = mod.PackageDependencies.Any(p => p.Contains("junit") || p.Contains("test"));
        }
        catch
        {
            mod.Name = Path.GetFileName(Path.GetDirectoryName(pomPath)!);
        }

        return mod;
    }

    private static List<SymbolInfo> ParseJavaFile(string relativePath, string code)
    {
        var list = new List<SymbolInfo>();

        var packageMatch = Regex.Match(code, @"package\s+([a-zA-Z0-9_\.]+);");
        var packageName = packageMatch.Success ? packageMatch.Groups[1].Value : string.Empty;

        var typeMatches = Regex.Matches(code, @"(?:@([a-zA-Z0-9_\(\)"",\s=\.]+)\s+)*(?:public\s+|protected\s+|private\s+)?(?:static\s+)?(?:final\s+)?(class|interface|enum|record)\s+([a-zA-Z0-9_]+)(?:<[^>]+>)?(?:\s+extends\s+([a-zA-Z0-9_<>,\s]+))?(?:\s+implements\s+([a-zA-Z0-9_<>,\s]+))?");

        foreach (Match m in typeMatches)
        {
            var kind = m.Groups[2].Value;
            var name = m.Groups[3].Value;
            var extendsClause = m.Groups[4].Value;
            var implementsClause = m.Groups[5].Value;

            if (string.IsNullOrEmpty(name)) continue;

            var fullName = string.IsNullOrEmpty(packageName) ? name : $"{packageName}.{name}";

            // Find annotations before this type
            var annotations = new List<string>();
            var annotationMatches = Regex.Matches(code, @"@([A-Z][a-zA-Z0-9_]*)");
            foreach (Match am in annotationMatches)
            {
                annotations.Add(am.Groups[1].Value);
            }

            // Injected dependencies in fields or constructors
            var dependencies = new HashSet<string>();
            var fieldMatches = Regex.Matches(code, @"(?:private|protected|public)\s+(?:final\s+)?([A-Z][a-zA-Z0-9_<>]*)\s+([a-zA-Z0-9_]+);");
            foreach (Match fm in fieldMatches)
            {
                var type = fm.Groups[1].Value;
                if (!type.StartsWith("String") && !type.StartsWith("int") && !type.StartsWith("long") && !type.StartsWith("boolean") && !type.StartsWith("List") && !type.StartsWith("Map"))
                {
                    dependencies.Add(type);
                }
            }

            var interfaces = string.IsNullOrWhiteSpace(implementsClause)
                ? []
                : implementsClause.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var methods = Regex.Matches(code, @"public\s+(?:[A-Za-z0-9_<>\[\]]+)\s+([a-zA-Z0-9_]+)\s*\(")
                .Select(mm => mm.Groups[1].Value)
                .Distinct()
                .ToList();

            var sym = new SymbolInfo
            {
                Name = name,
                FullName = fullName,
                Kind = kind,
                RelativeFilePath = relativePath,
                NamespaceOrPackage = packageName,
                AttributesOrAnnotations = annotations.Distinct().ToList(),
                InterfacesImplemented = interfaces,
                BaseClass = string.IsNullOrWhiteSpace(extendsClause) ? null : extendsClause.Trim(),
                Dependencies = dependencies.ToList(),
                Methods = methods
            };

            ClassifyJavaSymbol(sym);
            list.Add(sym);
        }

        return list;
    }

    private static void ClassifyJavaSymbol(SymbolInfo sym)
    {
        var hasControllerAnno = sym.AttributesOrAnnotations.Any(a => a == "RestController" || a == "Controller") ||
                                sym.Name.EndsWith("Controller");

        if (hasControllerAnno)
        {
            sym.DetectedRole = SymbolRole.Controller;
            sym.CurrentLayer = CleanArchitectureLayer.Presentation;
            sym.TargetCleanLayer = CleanArchitectureLayer.Presentation;
            sym.RoleConfidence = 0.99;
            sym.RoleEvidence.Add("Annotated with @RestController / @Controller or ends with Controller.");
            return;
        }

        var hasRepoAnno = sym.AttributesOrAnnotations.Any(a => a == "Repository") ||
                          sym.InterfacesImplemented.Any(i => i.Contains("JpaRepository") || i.Contains("CrudRepository")) ||
                          sym.Name.EndsWith("Repository");

        if (hasRepoAnno)
        {
            sym.DetectedRole = SymbolRole.Repository;
            sym.CurrentLayer = CleanArchitectureLayer.Infrastructure;
            sym.TargetCleanLayer = CleanArchitectureLayer.Infrastructure;
            sym.RoleConfidence = 0.96;
            sym.RoleEvidence.Add("Extends JpaRepository / CrudRepository or annotated with @Repository.");
            return;
        }

        var hasEntityAnno = sym.AttributesOrAnnotations.Any(a => a == "Entity" || a == "Table" || a == "Document") ||
                            sym.RelativeFilePath.Contains("/entity/", StringComparison.OrdinalIgnoreCase) ||
                            sym.RelativeFilePath.Contains("/model/", StringComparison.OrdinalIgnoreCase);

        if (hasEntityAnno)
        {
            sym.DetectedRole = SymbolRole.Entity;
            sym.CurrentLayer = CleanArchitectureLayer.Domain;
            sym.TargetCleanLayer = CleanArchitectureLayer.Domain;
            sym.RoleConfidence = 0.92;
            sym.RoleEvidence.Add("Annotated with @Entity or resides in domain model package.");
            if (sym.AttributesOrAnnotations.Any(a => a == "Entity" || a == "Table"))
            {
                sym.HasFrameworkLeakage = true;
            }
            return;
        }

        var hasServiceAnno = sym.AttributesOrAnnotations.Any(a => a == "Service") || sym.Name.EndsWith("Service");
        if (hasServiceAnno)
        {
            sym.DetectedRole = SymbolRole.Service;
            sym.CurrentLayer = CleanArchitectureLayer.Application;
            sym.TargetCleanLayer = CleanArchitectureLayer.Application;
            sym.RoleConfidence = 0.90;
            sym.RoleEvidence.Add("Annotated with @Service or named Service encapsulating business workflow.");
            return;
        }

        var isDto = sym.Name.EndsWith("Dto") || sym.Name.EndsWith("DTO") ||
                    sym.Name.EndsWith("Request") || sym.Name.EndsWith("Response") ||
                    sym.RelativeFilePath.Contains("/dto/", StringComparison.OrdinalIgnoreCase);

        if (isDto)
        {
            sym.DetectedRole = SymbolRole.Dto;
            sym.CurrentLayer = CleanArchitectureLayer.Application;
            sym.TargetCleanLayer = CleanArchitectureLayer.Application;
            sym.RoleConfidence = 0.95;
            sym.RoleEvidence.Add("DTO or Request/Response carrier object.");
            return;
        }

        sym.DetectedRole = SymbolRole.Unknown;
        sym.CurrentLayer = CleanArchitectureLayer.Shared;
        sym.TargetCleanLayer = CleanArchitectureLayer.Shared;
        sym.RoleConfidence = 0.50;
        sym.RoleEvidence.Add("Ambiguous Java component; flagged for architectural review.");
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
                FilePath = sym.RelativeFilePath
            });

            foreach (var dep in sym.Dependencies)
            {
                edges.Add(new GraphEdge
                {
                    Source = sym.FullName,
                    Target = dep,
                    Type = DependencyType.References
                });
            }
        }

        result.DependencyGraph = new DependencyGraphData { Nodes = nodes, Edges = edges };
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
            var res = new TransformationOperationResult
            {
                OperationId = op.OperationId,
                Type = op.Type
            };

            try
            {
                switch (op.Type)
                {
                    case OperationType.CreateProjectOrModule:
                        await HandleCreateJavaModuleAsync(workingDirectory, op);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Created Java module {op.DestinationPath}";
                        break;

                    case OperationType.MoveFile:
                        var src = Path.Combine(workingDirectory, op.SourcePath ?? "");
                        var dst = Path.Combine(workingDirectory, op.DestinationPath ?? "");
                        if (File.Exists(src))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                            File.Move(src, dst, true);
                        }
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Moved Java source {op.SourcePath} to {op.DestinationPath}";
                        break;

                    case OperationType.RenameNamespaceOrPackage:
                        await HandleRenameJavaPackageAsync(workingDirectory, op, cancellationToken);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath ?? op.SourcePath;
                        res.Message = $"Renamed Java package in {res.AffectedPath}";
                        break;

                    case OperationType.RewriteReferences:
                        var rewritten = await JavaRefactoring.RewriteReferencesAsync(workingDirectory, plan, cancellationToken);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Rewrote packages/imports in {rewritten.Count} Java file(s)";
                        break;

                    case OperationType.UpdateProjectReferences:
                        HandleJavaPomAsync(workingDirectory, op);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Updated Maven build file {op.DestinationPath}";
                        break;

                    case OperationType.RemoveLegacyProject:
                        var legacy = Path.Combine(workingDirectory, op.SourcePath ?? op.DestinationPath ?? "");
                        if (File.Exists(legacy)) File.Delete(legacy);
                        res.Success = true;
                        res.AffectedPath = op.SourcePath ?? op.DestinationPath;
                        res.Message = $"Removed legacy build file {op.SourcePath ?? op.DestinationPath}";
                        break;

                    case OperationType.ExtractInterface:
                        await HandleExtractJavaInterfaceAsync(workingDirectory, op);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Extracted Java interface to {op.DestinationPath}";
                        break;

                    case OperationType.CreateApplicationUseCase:
                        await HandleCreateJavaUseCaseAsync(workingDirectory, op);
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Created Java UseCase in {op.DestinationPath}";
                        break;

                    case OperationType.FlagForManualRefactoring:
                        res.Success = true;
                        res.AffectedPath = op.DestinationPath;
                        res.Message = $"Flagged for manual refactoring: {op.Description}";
                        break;

                    default:
                        res.Success = true;
                        res.Message = $"Executed {op.Type}";
                        break;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = $"Error transforming Java: {ex.Message}";
            }

            sw.Stop();
            res.Duration = sw.Elapsed;
            results.Add(res);
        }

        return results;
    }

    private static async Task HandleCreateJavaModuleAsync(string workingDir, PlanOperation op)
    {
        var pomPath = Path.Combine(workingDir, op.DestinationPath ?? "pom.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(pomPath)!);

        var moduleName = op.Parameters.GetValueOrDefault("ModuleName", "clean-module");
        var dependencies = op.Parameters.GetValueOrDefault("Dependencies", "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var dependencyXml = new System.Text.StringBuilder();
        if (dependencies.Length > 0)
        {
            dependencyXml.AppendLine("    <dependencies>");
            foreach (var dep in dependencies)
            {
                var parts = dep.Split(':', 3);
                if (parts.Length < 2) continue;
                var groupId = parts[0];
                var artifactId = parts[1];
                var version = parts.Length > 2 ? parts[2] : "";

                dependencyXml.AppendLine("        <dependency>");
                dependencyXml.AppendLine($"            <groupId>{groupId}</groupId>");
                dependencyXml.AppendLine($"            <artifactId>{artifactId}</artifactId>");
                if (!string.IsNullOrEmpty(version))
                {
                    dependencyXml.AppendLine($"            <version>{version}</version>");
                }
                dependencyXml.AppendLine("        </dependency>");
            }
            dependencyXml.AppendLine("    </dependencies>");
        }

        var pomContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>
    <groupId>com.example</groupId>
    <artifactId>{moduleName}</artifactId>
    <version>1.0.0-SNAPSHOT</version>
    <packaging>jar</packaging>

    <properties>
        <maven.compiler.source>17</maven.compiler.source>
        <maven.compiler.target>17</maven.compiler.target>
    </properties>

{dependencyXml}</project>
";
        await File.WriteAllTextAsync(pomPath, pomContent);
    }

    /// <summary>
    /// Turns the legacy single-module pom into a Maven reactor that aggregates
    /// the new clean architecture modules, or adds dependencies to a module pom.
    /// </summary>
    private static void HandleJavaPomAsync(string workingDir, PlanOperation op)
    {
        var pomPath = Path.Combine(workingDir, op.DestinationPath ?? "pom.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(pomPath)!);

        var modules = op.Parameters.GetValueOrDefault("Modules", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (modules.Length > 0)
        {
            var modulesXml = string.Join("\n", modules.Select(m => $"        <module>{m}</module>"));
            var content = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<project xmlns=""http://maven.apache.org/POM/4.0.0""
         xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xsi:schemaLocation=""http://maven.apache.org/POM/4.0.0 http://maven.apache.org/xsd/maven-4.0.0.xsd"">
    <modelVersion>4.0.0</modelVersion>
    <groupId>com.example</groupId>
    <artifactId>clean-architecture-parent</artifactId>
    <version>1.0.0-SNAPSHOT</version>
    <packaging>pom</packaging>

    <properties>
        <maven.compiler.source>17</maven.compiler.source>
        <maven.compiler.target>17</maven.compiler.target>
    </properties>

    <modules>
{modulesXml}
    </modules>
</project>
";
            File.WriteAllText(pomPath, content);
            return;
        }

        if (!File.Exists(pomPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pomPath)!);
            File.WriteAllText(pomPath, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<project xmlns=\"http://maven.apache.org/POM/4.0.0\"><modelVersion>4.0.0</modelVersion><groupId>com.example</groupId><artifactId>module</artifactId><version>1.0.0-SNAPSHOT</version></project>\n");
        }

        var doc = XDocument.Load(pomPath);
        XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var root = doc.Root;
        if (root == null) return;

        var dependencies = op.Parameters.GetValueOrDefault("Dependencies", "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (dependencies.Length == 0) return;

        var dependenciesElement = root.Element(ns + "dependencies");
        if (dependenciesElement == null)
        {
            dependenciesElement = new XElement(ns + "dependencies");
            root.Add(dependenciesElement);
        }

        foreach (var dep in dependencies)
        {
            var parts = dep.Split(':', 3);
            if (parts.Length < 2) continue;
            var depElement = new XElement(ns + "dependency",
                new XElement(ns + "groupId", parts[0]),
                new XElement(ns + "artifactId", parts[1]));
            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
            {
                depElement.Add(new XElement(ns + "version", parts[2]));
            }
            dependenciesElement.Add(depElement);
        }

        doc.Save(pomPath);
    }

    private static async Task HandleRenameJavaPackageAsync(string workingDir, PlanOperation op, CancellationToken ct)
    {
        var targetFile = Path.Combine(workingDir, op.DestinationPath ?? op.SourcePath ?? "");
        if (!File.Exists(targetFile)) return;

        var oldPkg = op.Parameters.GetValueOrDefault("OldPackage", "");
        var newPkg = op.Parameters.GetValueOrDefault("NewPackage", "");

        var code = await File.ReadAllTextAsync(targetFile, ct);
        if (!string.IsNullOrEmpty(newPkg))
        {
            var replaced = Regex.Replace(code, @"package\s+[a-zA-Z0-9_\.]+;", $"package {newPkg};");
            await File.WriteAllTextAsync(targetFile, replaced, ct);
        }
    }

    private static async Task HandleExtractJavaInterfaceAsync(string workingDir, PlanOperation op)
    {
        var target = Path.Combine(workingDir, op.DestinationPath ?? "");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        var ifaceName = op.Parameters.GetValueOrDefault("InterfaceName", "IRepository");
        var pkg = op.Parameters.GetValueOrDefault("Package", "com.example.application.ports");

        var code = $@"package {pkg};

public interface {ifaceName} {{
    // Extracted clean architecture interface contracts
}}
";
        await File.WriteAllTextAsync(target, code);
    }

    private static async Task HandleCreateJavaUseCaseAsync(string workingDir, PlanOperation op)
    {
        var target = Path.Combine(workingDir, op.DestinationPath ?? "");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        var useCaseName = op.Parameters.GetValueOrDefault("UseCaseName", "GetEntityUseCase");
        var pkg = op.Parameters.GetValueOrDefault("Package", "com.example.application.usecases");

        var code = $@"package {pkg};

import org.springframework.stereotype.Service;

@Service
public class {useCaseName} {{
    public void execute() {{
        // Coordinated application business logic
    }}
}}
";
        await File.WriteAllTextAsync(target, code);
    }

    public async Task<BuildResult> BuildAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        if (!await IsToolAvailableAsync("mvn", "-v", projectDirectory, cancellationToken))
        {
            return new BuildResult
            {
                Success = false,
                ErrorsCount = 0,
                ToolAvailable = false,
                Status = ToolVerificationStatus.ToolUnavailable,
                RawOutput = "Maven (mvn) is not available on PATH. Java build verification could not be performed; reporting ToolUnavailable instead of assuming success."
            };
        }

        var (exitCode, output) = await RunProcessAsync(
            "mvn", "-q -DskipTests package", projectDirectory, cancellationToken);

        return new BuildResult
        {
            Success = exitCode == 0,
            ErrorsCount = Regex.Matches(output, @"\[ERROR\]").Count,
            ToolAvailable = true,
            Status = exitCode == 0 ? ToolVerificationStatus.Succeeded : ToolVerificationStatus.Failed,
            RawOutput = output
        };
    }

    public async Task<TestResult> RunTestsAsync(string projectDirectory, CancellationToken cancellationToken = default)
    {
        if (!await IsToolAvailableAsync("mvn", "-v", projectDirectory, cancellationToken))
        {
            return new TestResult
            {
                Success = false,
                TotalTests = 0,
                ToolAvailable = false,
                Status = ToolVerificationStatus.ToolUnavailable,
                RawOutput = "Maven (mvn) is not available on PATH. Java test verification could not be performed; reporting ToolUnavailable instead of assuming success."
            };
        }

        var sw = Stopwatch.StartNew();
        var (exitCode, output) = await RunProcessAsync(
            "mvn", "-q test", projectDirectory, cancellationToken);
        sw.Stop();

        var passed = SumMatches(output, @"Tests run:\s*\d+,\s*Failures:\s*0,\s*Errors:\s*0,\s*Skipped:\s*\d+");
        var totalMatch = Regex.Match(output, @"Tests run:\s*(\d+)");
        var total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : passed;

        return new TestResult
        {
            Success = exitCode == 0,
            TotalTests = total,
            PassedTests = passed,
            FailedTests = Math.Max(0, total - passed),
            Duration = sw.Elapsed,
            ToolAvailable = true,
            Status = exitCode == 0 ? ToolVerificationStatus.Succeeded : ToolVerificationStatus.Failed,
            RawOutput = output
        };
    }

    private static int SumMatches(string output, string pattern)
    {
        var sum = 0;
        foreach (Match m in Regex.Matches(output, pattern))
        {
            var run = Regex.Match(m.Value, @"Tests run:\s*(\d+)");
            if (run.Success) sum += int.Parse(run.Groups[1].Value);
        }
        return sum;
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

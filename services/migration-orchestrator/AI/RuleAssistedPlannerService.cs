using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.Orchestrator.AI;

public sealed class RuleAssistedPlannerService : ILLMService
{
    public Task<MigrationPlan> GenerateMigrationPlanAsync(
        ProjectAnalysisResult analysis,
        string? customInstructions = null,
        CancellationToken cancellationToken = default)
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid(),
            JobId = analysis.JobId,
            PlanVersion = 1,
            SourceHash = analysis.SourceHash,
            TargetArchitecture = analysis.Language == LanguageKind.CSharp
                ? TargetArchitecture.DotNetCleanArchitecture
                : TargetArchitecture.JavaCleanArchitecture,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (analysis.Language == LanguageKind.CSharp)
        {
            GenerateDotNetPlan(plan, analysis);
        }
        else
        {
            GenerateJavaPlan(plan, analysis);
        }

        return Task.FromResult(plan);
    }

    // Destination directories keyed by the canonical target namespace assigned by
    // the analyzer's semantic classifier. Never apply raw path-substring tricks.
    private static readonly Dictionary<string, string> NamespaceToDirectory = new(StringComparer.Ordinal)
    {
        ["Project.Domain.Entities"] = "src/Project.Domain/Entities",
        ["Project.Application.Common.Dtos"] = "src/Project.Application/Common/Dtos",
        ["Project.Application.Common.Interfaces"] = "src/Project.Application/Common/Interfaces",
        ["Project.Application.Common.Exceptions"] = "src/Project.Application/Common/Exceptions",
        ["Project.Application.Common.Responses"] = "src/Project.Application/Common/Responses",
        ["Project.Application.Common.Webhooks"] = "src/Project.Application/Common/Webhooks",
        ["Project.Application.Common.ETag"] = "src/Project.Application/Common/ETag",
        ["Project.Application.Validators"] = "src/Project.Application/Validators",
        ["Project.Application.Services"] = "src/Project.Application/Services",
        ["Project.Application.Authentication"] = "src/Project.Application/Authentication",
        ["Project.Infrastructure.Persistence"] = "src/Project.Infrastructure/Persistence",
        ["Project.Infrastructure.Persistence.Configurations"] = "src/Project.Infrastructure/Persistence/Configurations",
        ["Project.Infrastructure.Persistence.Migrations"] = "src/Project.Infrastructure/Persistence/Migrations",
        ["Project.Infrastructure.Common"] = "src/Project.Infrastructure/Common",
        ["Project.Infrastructure.Webhooks"] = "src/Project.Infrastructure/Webhooks",
        ["Project.API.Controllers"] = "src/Project.API/Controllers",
        ["Project.API.Filters"] = "src/Project.API/Filters",
        ["Project.API.Settings"] = "src/Project.API/Settings",
        ["Project.API.Middleware"] = "src/Project.API/Middleware",
        ["Project.API"] = "src/Project.API"
    };

    private static string ProjectOfNamespace(string ns)
    {
        if (ns.StartsWith("Project.Domain", StringComparison.Ordinal)) return "Domain";
        if (ns.StartsWith("Project.Application", StringComparison.Ordinal)) return "Application";
        if (ns.StartsWith("Project.Infrastructure", StringComparison.Ordinal)) return "Infrastructure";
        if (ns.StartsWith("Project.API", StringComparison.Ordinal)) return "API";
        return "";
    }

    private static void GenerateDotNetPlan(MigrationPlan plan, ProjectAnalysisResult analysis)
    {
        var ops = new List<PlanOperation>();
        int seq = 1;

        plan.Summary = "Migration of layered .NET monolithic application into Clean Architecture with Domain, Application, Infrastructure, and API projects.";
        plan.OverallRisk = analysis.Violations.Any(v => v.Severity == ViolationSeverity.Critical) ? RiskLevel.High : RiskLevel.Medium;
        plan.EstimatedEffortHours = 6.0;

        // The legacy packages are the source of truth for what the migrated
        // projects must reference so the relocated code still compiles.
        var packages = analysis.Modules
            .SelectMany(m => m.PackageDependencies)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var infraPackages = packages
            .Where(p => IsInfrastructurePackage(p))
            .ToList();

        // The synthesized Infrastructure DI extension needs a concrete EF Core
        // provider. Mirror the EF Core version already referenced by the app and
        // default to SQLite when no provider-specific package is present.
        var hasProvider = infraPackages.Any(p => NameOf(p) is { } n &&
            (n.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ||
             n.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ||
             n.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)));
        if (!hasProvider)
        {
            var efVersion = packages
                .Where(p => "Microsoft.EntityFrameworkCore".Equals(NameOf(p), StringComparison.OrdinalIgnoreCase))
                .Select(p => VersionOf(p))
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));
            infraPackages.Add($"Microsoft.EntityFrameworkCore.Sqlite|{efVersion ?? "10.0.12"}");
        }

        var appPackages = packages
            .Where(p => IsApplicationPackage(p))
            .ToList();

        // Step 1: Scaffold the four clean architecture projects. Application and
        // Infrastructure receive a FrameworkReference so host abstractions
        // (ILogger, IOptions, IHostedService, IHttpClientFactory, DI) resolve.
        ops.Add(CreateProject(seq++, "src/Project.Domain/Project.Domain.csproj", "Library", null, null,
            "Domain encapsulates enterprise business rules, entities, and value objects with pure POCO models."));
        ops.Add(CreateProject(seq++, "src/Project.Application/Project.Application.csproj", "Library",
            string.Join(";", appPackages), "Microsoft.AspNetCore.App",
            "Application coordinates use cases and defines repository abstractions."));
        ops.Add(CreateProject(seq++, "src/Project.Infrastructure/Project.Infrastructure.csproj", "Library",
            string.Join(";", infraPackages), "Microsoft.AspNetCore.App",
            "Infrastructure implements repository interfaces, EF Core DbContext, and external services."));
        ops.Add(CreateProject(seq++, "src/Project.API/Project.API.csproj", "Exe", string.Join(";", packages), null,
            "Presentation layer hosts controllers and the dependency injection composition root."));

        // Step 2: Group symbols by source file and relocate each file exactly
        // once, using the analyzer-computed target namespace. A file whose
        // symbols disagree across *projects* cannot be auto-split safely.
        var files = new Dictionary<string, FileTarget>(StringComparer.OrdinalIgnoreCase);
        var unclassified = new List<SymbolInfo>();

        foreach (var sym in analysis.Symbols)
        {
            if (string.IsNullOrEmpty(sym.RelativeFilePath)) continue;
            if (string.IsNullOrEmpty(sym.TargetNamespace))
            {
                unclassified.Add(sym);
                continue;
            }
            if (!NamespaceToDirectory.TryGetValue(sym.TargetNamespace, out var dir))
            {
                unclassified.Add(sym);
                continue;
            }

            if (!files.TryGetValue(sym.RelativeFilePath, out var existing))
            {
                files[sym.RelativeFilePath] = new FileTarget(
                    sym.RelativeFilePath,
                    dir,
                    sym.TargetNamespace,
                    sym.NamespaceOrPackage ?? "");
            }
            else if (!string.Equals(existing.Directory, dir, StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(ProjectOfNamespace(existing.TargetNamespace), ProjectOfNamespace(sym.TargetNamespace), StringComparison.OrdinalIgnoreCase))
            {
                // A single file holds symbols that belong to different projects;
                // this cannot be auto-split safely, so flag it for a human.
                ops.Add(FlagOperation(seq++, sym.RelativeFilePath,
                    $"File '{sym.RelativeFilePath}' contains '{sym.Name}' which maps to a different project ({dir}) than the rest of the file. Manual split required."));
                files.Remove(sym.RelativeFilePath);
            }
        }

        foreach (var file in files.Values.OrderBy(f => f.Source, StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(file.Source);
            var destination = $"{file.Directory}/{fileName}";

            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.MoveFile,
                Description = $"Move {fileName} to {file.Directory}",
                SourcePath = file.Source,
                DestinationPath = destination,
                Reason = "Relocate source file into the owning clean architecture layer.",
                Confidence = 0.97,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });

            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.RenameNamespaceOrPackage,
                Description = $"Rewrite namespace of {fileName} to {file.TargetNamespace}",
                SourcePath = destination,
                DestinationPath = destination,
                Parameters = new()
                {
                    ["OldNamespace"] = file.OldNamespace,
                    ["NewNamespace"] = file.TargetNamespace
                },
                Reason = "Align namespace with the owning assembly boundary.",
                Confidence = 0.98,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true,
                CodeDiffPreview = $"- namespace {file.OldNamespace};\n+ namespace {file.TargetNamespace};"
            });
        }

        // Preserve an existing entry-point (Program.cs with top-level statements)
        // verbatim instead of synthesising a new host that would lose DI
        // registrations, migrations, and middleware configuration.
        var hasProgramCs = analysis.SourceFiles.Any(s =>
            string.Equals(Path.GetFileName(s.RelativePath), "Program.cs", StringComparison.OrdinalIgnoreCase));

        if (hasProgramCs)
        {
            var programSource = analysis.SourceFiles.First(s =>
                string.Equals(Path.GetFileName(s.RelativePath), "Program.cs", StringComparison.OrdinalIgnoreCase)).RelativePath;
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.MoveFile,
                Description = "Preserve the existing Program.cs entry point into the API host",
                SourcePath = programSource,
                DestinationPath = "src/Project.API/Program.cs",
                Reason = "The existing composition root already wires all DI registrations, middleware and providers; regenerating it would lose behavior.",
                Confidence = 0.99,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        // Step 3: Abstract only the repository implementations that do not
        // already have an Application-layer interface. Pre-existing interfaces
        // (real-world apps) must be reused as-is; abstract base repositories are
        // intentionally skipped because extracting an interface would break
        // inheritance chains.
        var interfaceNames = analysis.Symbols
            .Where(s => s.Kind == "interface")
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);
        var usedBaseClasses = analysis.Symbols
            .Where(s => !string.IsNullOrWhiteSpace(s.BaseClass))
            .Select(s => SimpleName(s.BaseClass!))
            .ToHashSet(StringComparer.Ordinal);

        var domainUsings = analysis.Symbols
            .Where(s => s.DetectedRole is SymbolRole.Entity or SymbolRole.ValueObject or SymbolRole.Dto)
            .Select(s => s.TargetNamespace)
            .Where(ns => !string.IsNullOrEmpty(ns))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var usingsParameter = string.Join(";", domainUsings);

        foreach (var repo in analysis.Symbols.Where(s => s.DetectedRole == SymbolRole.Repository))
        {
            if (!files.TryGetValue(repo.RelativeFilePath, out var file)) continue;
            if (usedBaseClasses.Contains(repo.Name)) continue; // abstract/inherited base repo
            var interfaceName = repo.Name.StartsWith('I') ? repo.Name : $"I{repo.Name}";
            if (interfaceNames.Contains(interfaceName)) continue; // already abstracted

            var implPath = $"{file.Directory}/{Path.GetFileName(repo.RelativeFilePath)}";
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.ExtractInterface,
                Description = $"Extract {interfaceName} from {repo.Name} and invert its dependencies",
                SourcePath = implPath,
                DestinationPath = $"src/Project.Application/Common/Interfaces/{interfaceName}.cs",
                Parameters = new()
                {
                    ["ImplementationClass"] = repo.Name,
                    ["ImplementationPath"] = implPath,
                    ["InterfaceName"] = interfaceName,
                    ["Namespace"] = "Project.Application.Common.Interfaces",
                    ["Usings"] = usingsParameter
                },
                Reason = "Dependency Inversion Principle: consumers depend on an abstraction owned by the Application layer.",
                Evidence = repo.RoleEvidence,
                Confidence = 0.95,
                Risk = RiskLevel.Medium,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        // Step 4: Re-target every using directive and invert concrete repository
        // references across the whole compilation.
        ops.Add(new PlanOperation
        {
            Sequence = seq++,
            Type = OperationType.RewriteReferences,
            Description = "Rewrite namespaces, using directives, and invert concrete repository references",
            DestinationPath = "src",
            Reason = "Ensures every reference points at the new namespace and abstractions.",
            Confidence = 0.95,
            Risk = RiskLevel.Medium,
            RequiresApproval = true,
            IsApproved = true
        });

        // Step 5: Project references (dependency direction).
        ops.Add(AddProjectReferences(seq++, "src/Project.Application/Project.Application.csproj",
            "..\\Project.Domain\\Project.Domain.csproj", "Application uses Domain models."));
        ops.Add(AddProjectReferences(seq++, "src/Project.Infrastructure/Project.Infrastructure.csproj",
            "..\\Project.Application\\Project.Application.csproj,..\\Project.Domain\\Project.Domain.csproj",
            "Infrastructure implements Application interfaces over Domain entities."));
        ops.Add(AddProjectReferences(seq++, "src/Project.API/Project.API.csproj",
            "..\\Project.Application\\Project.Application.csproj,..\\Project.Infrastructure\\Project.Infrastructure.csproj,..\\Project.Domain\\Project.Domain.csproj",
            "API composes Application use cases through Infrastructure and exposes Domain models."));

        // Step 6: Composition root and DI. Only synthesised when no Program.cs
        // exists; otherwise the preserved entry point keeps ownership.
        if (!hasProgramCs)
        {
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.UpdateDependencyInjection,
                Description = "Register DbContext and repositories in Infrastructure DI extension",
                DestinationPath = "src/Project.Infrastructure/DependencyInjection.cs",
                Parameters = new() { ["Layer"] = "Infrastructure", ["Namespace"] = "Project.Infrastructure" },
                Reason = "Infrastructure owns framework wiring for persistence.",
                Confidence = 0.95,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });

            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.UpdateDependencyInjection,
                Description = "Generate the API composition root (Program.cs)",
                DestinationPath = "src/Project.API/Program.cs",
                Parameters = new()
                {
                    ["Layer"] = "Presentation",
                    ["Namespace"] = "Project.API",
                    ["InfrastructureNamespace"] = "Project.Infrastructure"
                },
                Reason = "The presentation host must compose all layers without the domain knowing about them.",
                Confidence = 0.95,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        // Step 7: Retire the superseded monolithic project so it no longer
        // double-compiles the relocated sources, and drop build artifacts or
        // macOS archive metadata that would otherwise break the new build.
        foreach (var noise in EnumerateNoisePaths(analysis))
        {
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.RemoveLegacyProject,
                Description = $"Remove build/archive noise {noise}",
                SourcePath = noise,
                Reason = "bin/obj outputs and macOS __MACOSX/.DS_Store/AppleDouble metadata must not ship or compile.",
                Confidence = 1.0,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        foreach (var module in analysis.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.RelativePath)) continue;
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.RemoveLegacyProject,
                Description = $"Remove superseded legacy project {module.RelativePath}",
                SourcePath = module.RelativePath,
                Reason = "The legacy project would otherwise recompile the relocated files and break the build.",
                Confidence = 1.0,
                Risk = RiskLevel.Medium,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        foreach (var sym in unclassified.Where(s => !string.IsNullOrEmpty(s.RelativeFilePath)))
        {
            ops.Add(FlagOperation(seq++, sym.RelativeFilePath,
                $"Symbol '{sym.Name}' in '{sym.RelativeFilePath}' could not be classified into a clean architecture layer."));
        }

        ops.Add(new PlanOperation
        {
            Sequence = seq++,
            Type = OperationType.AddArchitectureValidationRules,
            Description = "Add automated architecture constraint rules to prevent future layering violations",
            DestinationPath = "src/Project.API/Architecture.cs",
            Reason = "Enforces that Presentation never directly references Infrastructure, and Domain has zero dependencies.",
            Confidence = 1.0,
            Risk = RiskLevel.Low,
            RequiresApproval = true,
            IsApproved = true
        });

        plan.Operations = ops;
    }

    private static IEnumerable<string> EnumerateNoisePaths(ProjectAnalysisResult analysis)
    {
        var noise = new List<string>();

        foreach (var file in analysis.SourceFiles.Select(s => s.RelativePath))
        {
            var norm = file.Replace('\\', '/');
            var fileName = Path.GetFileName(norm);
            if (fileName.StartsWith("._", StringComparison.Ordinal) ||
                string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase) ||
                norm.Contains("/__MACOSX/", StringComparison.OrdinalIgnoreCase))
            {
                noise.Add(file);
            }
        }

        // Whole noise directories referenced indirectly through source files.
        if (noise.Any(n => n.Replace('\\', '/').Contains("/__MACOSX", StringComparison.OrdinalIgnoreCase)))
        {
            noise.Add("__MACOSX");
        }

        return noise.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsInfrastructurePackage(string package)
    {
        var lower = package.ToLowerInvariant();
        return lower.Contains("entityframeworkcore") ||
               lower.Contains("npgsql") ||
               lower.Contains("dapper") ||
               lower.Contains("microsoft.data.sqlclient") ||
               lower.Contains("stackexchange.redis") ||
               lower.Contains("polly") ||
               lower.Contains("refit") ||
               lower.Contains("mongodb") ||
               lower.Contains("bcrypt");
    }

    private static bool IsApplicationPackage(string package)
    {
        var lower = package.ToLowerInvariant();
        return lower.Contains("fluentvalidation") ||
               lower.Contains("system.identitymodel.tokens.jwt") ||
               lower.Contains("microsoft.identitymodel") ||
               lower.Contains("bcrypt") ||
               lower.Contains("mediatr") ||
               lower.Contains("automapper") ||
               lower.Contains("newtonsoft.json");
    }

    private static string SimpleName(string type)
    {
        var t = type.Trim();
        var generic = t.IndexOf('<');
        if (generic >= 0) t = t[..generic];
        var dot = t.LastIndexOf('.');
        if (dot >= 0) t = t[(dot + 1)..];
        return t.Trim();
    }

    private static string NameOf(string package)
    {
        if (package.Contains('|')) return package.Split('|')[0].Trim();
        var lastSpace = package.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            var candidate = package[(lastSpace + 1)..];
            if (candidate.Length > 0 && (char.IsDigit(candidate[0]) || candidate[0] == '['))
            {
                return package[..lastSpace].Trim();
            }
        }
        return package.Trim();
    }

    private static string VersionOf(string package)
    {
        if (package.Contains('|')) return package.Split('|', 2)[1].Trim();
        var lastSpace = package.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            var candidate = package[(lastSpace + 1)..];
            if (candidate.Length > 0 && (char.IsDigit(candidate[0]) || candidate[0] == '['))
            {
                return candidate.Trim();
            }
        }
        return string.Empty;
    }

    private static PlanOperation CreateProject(int seq, string destination, string outputType, string? packages, string? frameworkReference, string reason)
    {
        var parameters = new Dictionary<string, string>
        {
            ["TargetFramework"] = "net10.0",
            ["OutputType"] = outputType
        };
        if (!string.IsNullOrEmpty(packages)) parameters["Packages"] = packages;
        if (!string.IsNullOrEmpty(frameworkReference)) parameters["FrameworkReference"] = frameworkReference;

        return new PlanOperation
        {
            Sequence = seq,
            Type = OperationType.CreateProjectOrModule,
            Description = $"Create project {destination}",
            DestinationPath = destination,
            Parameters = parameters,
            Reason = reason,
            Confidence = 1.0,
            Risk = RiskLevel.Low,
            RequiresApproval = true,
            IsApproved = true
        };
    }

    private static PlanOperation AddProjectReferences(int seq, string destination, string references, string reason)
    {
        return new PlanOperation
        {
            Sequence = seq,
            Type = OperationType.UpdateProjectReferences,
            Description = $"Configure project references for {destination}",
            DestinationPath = destination,
            Parameters = new() { ["References"] = references },
            Reason = reason,
            Confidence = 1.0,
            Risk = RiskLevel.Low,
            RequiresApproval = true,
            IsApproved = true
        };
    }

    private static PlanOperation FlagOperation(int seq, string path, string description)
    {
        return new PlanOperation
        {
            Sequence = seq,
            Type = OperationType.FlagForManualRefactoring,
            Description = description,
            DestinationPath = path,
            Reason = "Automated transformation is unsafe for this case.",
            Confidence = 1.0,
            Risk = RiskLevel.High,
            RequiresApproval = true,
            IsApproved = true
        };
    }

    private sealed record FileTarget(string Source, string Directory, string TargetNamespace, string OldNamespace);

    private static void GenerateJavaPlan(MigrationPlan plan, ProjectAnalysisResult analysis)
    {
        var ops = new List<PlanOperation>();
        int seq = 1;

        plan.Summary = "Migration of monolithic Spring Boot application into Clean Architecture Maven multi-module structure (domain, application, infrastructure, api).";
        plan.OverallRisk = RiskLevel.Medium;
        plan.EstimatedEffortHours = 5.0;

        // Modules. Dependencies are wired so the reactor builds top-down.
        ops.Add(CreateJavaModule(seq++, "domain/pom.xml", "domain", null,
            "Core domain business entities with no framework coupling."));
        ops.Add(CreateJavaModule(seq++, "application/pom.xml", "application", "com.example:domain:1.0.0-SNAPSHOT",
            "Coordinates domain use cases and defines outbound repository ports."));
        ops.Add(CreateJavaModule(seq++, "infrastructure/pom.xml", "infrastructure",
            "com.example:application:1.0.0-SNAPSHOT;com.example:domain:1.0.0-SNAPSHOT;org.springframework.boot:spring-boot-starter-data-jpa:3.2.0",
            "Implements persistence adapters and external service clients."));
        ops.Add(CreateJavaModule(seq++, "api/pom.xml", "api",
            "com.example:application:1.0.0-SNAPSHOT;com.example:infrastructure:1.0.0-SNAPSHOT;org.springframework.boot:spring-boot-starter-web:3.2.0",
            "Hosts @RestController endpoints and the Spring Boot application class."));

        // Group symbols by source file so each file is moved once.
        var files = new Dictionary<string, (string Directory, string Package)>(StringComparer.OrdinalIgnoreCase);
        foreach (var sym in analysis.Symbols)
        {
            if (string.IsNullOrEmpty(sym.RelativeFilePath)) continue;
            var target = MapJavaSymbolToTarget(sym);
            if (target == null) continue;
            if (!files.ContainsKey(sym.RelativeFilePath))
            {
                files[sym.RelativeFilePath] = target.Value;
            }
        }

        foreach (var file in files.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            var fileName = Path.GetFileName(file.Key);
            var destination = $"{file.Value.Directory}/{fileName}";

            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.MoveFile,
                Description = $"Move {fileName} to {file.Value.Directory}",
                SourcePath = file.Key,
                DestinationPath = destination,
                Reason = "Relocate the source file into the owning clean architecture module.",
                Confidence = 0.95,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });

            var oldPackage = analysis.Symbols
                .FirstOrDefault(s => string.Equals(s.RelativeFilePath, file.Key, StringComparison.OrdinalIgnoreCase))
                ?.NamespaceOrPackage ?? "";

            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.RenameNamespaceOrPackage,
                Description = $"Update package of {fileName} to {file.Value.Package}",
                SourcePath = destination,
                DestinationPath = destination,
                Parameters = new() { ["OldPackage"] = oldPackage, ["NewPackage"] = file.Value.Package },
                Reason = "Align package with the owning module.",
                Confidence = 1.0,
                Risk = RiskLevel.Low,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        // Re-target every package declaration and import.
        ops.Add(new PlanOperation
        {
            Sequence = seq++,
            Type = OperationType.RewriteReferences,
            Description = "Rewrite Java package declarations and import statements",
            DestinationPath = ".",
            Reason = "Relocation only compiles if imports follow the new package layout.",
            Confidence = 0.95,
            Risk = RiskLevel.Medium,
            RequiresApproval = true,
            IsApproved = true
        });

        // The domain port extraction and JPA adapter inversion requires a JDK to
        // verify; it is deliberately escalated to a human rather than guessed.
        foreach (var repo in analysis.Symbols.Where(s => s.DetectedRole == SymbolRole.Repository))
        {
            ops.Add(new PlanOperation
            {
                Sequence = seq++,
                Type = OperationType.FlagForManualRefactoring,
                Description = $"Define a framework-free domain port for '{repo.Name}' and make the Spring Data interface implement it. " +
                              "The Application/API layers must depend on the port, not org.springframework.data.jpa.repository.JpaRepository.",
                SourcePath = repo.RelativeFilePath,
                Reason = "JPA adapter inversion cannot be verified without a JDK/Maven toolchain and must not be generated blindly.",
                Evidence = repo.RoleEvidence,
                Confidence = 1.0,
                Risk = RiskLevel.High,
                RequiresApproval = true,
                IsApproved = true
            });
        }

        // Replace the legacy single-module pom with a reactor aggregator.
        ops.Add(new PlanOperation
        {
            Sequence = seq++,
            Type = OperationType.UpdateProjectReferences,
            Description = "Convert the legacy pom.xml into a Maven reactor aggregator",
            DestinationPath = "pom.xml",
            Parameters = new() { ["Modules"] = "domain,application,infrastructure,api" },
            Reason = "The relocated sources must be driven by a multi-module reactor build.",
            Confidence = 1.0,
            Risk = RiskLevel.Medium,
            RequiresApproval = true,
            IsApproved = true
        });

        ops.Add(new PlanOperation
        {
            Sequence = seq++,
            Type = OperationType.AddArchitectureValidationRules,
            Description = "Add architecture validation rules for the Java modules",
            DestinationPath = "application/src/test/java/com/example/arch/ArchitectureTests.java",
            Reason = "Prevents future layering regressions once a JDK is available.",
            Confidence = 1.0,
            Risk = RiskLevel.Low,
            RequiresApproval = true,
            IsApproved = true
        });

        plan.Operations = ops;
    }

    private static PlanOperation CreateJavaModule(int seq, string destination, string moduleName, string? dependencies, string reason)
    {
        var parameters = new Dictionary<string, string> { ["ModuleName"] = moduleName };
        if (!string.IsNullOrEmpty(dependencies)) parameters["Dependencies"] = dependencies;

        return new PlanOperation
        {
            Sequence = seq,
            Type = OperationType.CreateProjectOrModule,
            Description = $"Create {moduleName} module",
            DestinationPath = destination,
            Parameters = parameters,
            Reason = reason,
            Confidence = 1.0,
            Risk = RiskLevel.Low,
            RequiresApproval = true,
            IsApproved = true
        };
    }

    private static (string Directory, string Package)? MapJavaSymbolToTarget(SymbolInfo sym) => sym.DetectedRole switch
    {
        SymbolRole.Controller => ("api/src/main/java/com/example/api", "com.example.api"),
        SymbolRole.Service or SymbolRole.DomainService =>
            ("application/src/main/java/com/example/application", "com.example.application"),
        SymbolRole.Repository or SymbolRole.Configuration =>
            ("infrastructure/src/main/java/com/example/infrastructure", "com.example.infrastructure"),
        SymbolRole.Entity or SymbolRole.ValueObject =>
            ("domain/src/main/java/com/example/domain", "com.example.domain"),
        SymbolRole.Dto =>
            ("application/src/main/java/com/example/application/dto", "com.example.application.dto"),
        SymbolRole.Interface =>
            ("application/src/main/java/com/example/application/ports", "com.example.application.ports"),
        _ => null
    };
}

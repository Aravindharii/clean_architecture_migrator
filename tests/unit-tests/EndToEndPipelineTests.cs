using CleanArchitectureMigration.Api.Services;
using CleanArchitectureMigration.ArchitectureRules;
using CleanArchitectureMigration.Contracts;
using CleanArchitectureMigration.DotNetWorker;
using CleanArchitectureMigration.JavaWorker;
using CleanArchitectureMigration.Orchestrator;
using CleanArchitectureMigration.Orchestrator.AI;
using Xunit;

namespace CleanArchitectureMigration.Tests;

public class EndToEndPipelineTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly DotNetWorker.DotNetWorker _dotNetWorker = new();
    private readonly JavaWorker.JavaWorker _javaWorker = new();
    private readonly RuleAssistedPlannerService _planner = new();
    private readonly ArchitectureRulesEngine _rulesEngine = new();
    private readonly SafeArchiveExtractor _extractor = new();

    public EndToEndPipelineTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "e2e_pipeline_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    private static string GetFixtureZipPath(string zipFileName)
    {
        var relativePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "../../../../../tests/migration-fixtures", zipFileName),
            Path.Combine(AppContext.BaseDirectory, "../../../../tests/migration-fixtures", zipFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "tests/migration-fixtures", zipFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "../tests/migration-fixtures", zipFileName)
        };

        foreach (var path in relativePaths)
        {
            var full = Path.GetFullPath(path);
            if (File.Exists(full)) return full;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "tests/migration-fixtures", zipFileName));
    }

    [Fact]
    public async Task EndToEnd_DotNetLayeredApp_CompletesAnalysisPlanApprovalAndTransformation()
    {
        var fixtureZipPath = GetFixtureZipPath("dotnet-layered-app.zip");
        Assert.True(File.Exists(fixtureZipPath), $"Fixture archive must exist at {fixtureZipPath}");

        // 1. Safe Extraction
        var extractDir = Path.Combine(_tempRoot, "extracted-source");
        using var zipStream = File.OpenRead(fixtureZipPath);
        var extraction = await _extractor.ExtractSafelyAsync(zipStream, extractDir);

        Assert.True(extraction.Success);
        Assert.NotEmpty(extraction.Sha256);
        Assert.True(extraction.FileCount >= 4);

        // 2. Static Analysis with Roslyn
        var jobId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var analysis = await _dotNetWorker.AnalyzeProjectAsync(extractDir, projectId, jobId, extraction.Sha256);

        Assert.Equal(LanguageKind.CSharp, analysis.Language);
        Assert.Contains(analysis.Symbols, s => s.Name == "OrdersController" && s.DetectedRole == SymbolRole.Controller);
        Assert.Contains(analysis.Symbols, s => s.Name == "Order" && s.DetectedRole == SymbolRole.Entity);
        Assert.Contains(analysis.Symbols, s => s.Name == "OrderRepository" && s.DetectedRole == SymbolRole.Repository);

        // 3. Architectural Rules Evaluation: Detects Violations
        var ruleResults = _rulesEngine.EvaluateAll(analysis);
        Assert.Contains(analysis.Violations, v => v.RuleId == "ARCH001" || v.RuleId == "ARCH004");

        // 4. AI Migration Plan Generation
        var plan = await _planner.GenerateMigrationPlanAsync(analysis);
        Assert.NotNull(plan);
        Assert.Equal(TargetArchitecture.DotNetCleanArchitecture, plan.TargetArchitecture);
        Assert.Contains(plan.Operations, o => o.Type == OperationType.CreateProjectOrModule && o.DestinationPath!.Contains("Project.Domain"));
        Assert.Contains(plan.Operations, o => o.Type == OperationType.CreateProjectOrModule && o.DestinationPath!.Contains("Project.Application"));
        Assert.Contains(plan.Operations, o => o.Type == OperationType.ExtractInterface && o.DestinationPath!.Contains("IOrderRepository.cs"));
        Assert.Contains(plan.Operations, o => o.Type == OperationType.RewriteReferences);
        Assert.Contains(plan.Operations, o => o.Type == OperationType.RemoveLegacyProject);

        // 5. Developer Approval Validation
        var orchestrator = new MigrationOrchestrator(_dotNetWorker, _javaWorker, _planner, _rulesEngine);
        var approvalRequest = new ApprovePlanRequest
        {
            SourceHash = extraction.Sha256,
            PlanVersion = plan.PlanVersion,
            ApprovedBy = "Senior Architect",
            Notes = "Approved migration to Clean Architecture"
        };
        var approval = await orchestrator.VerifyAndRecordApprovalAsync(jobId, plan, extraction.Sha256, approvalRequest);
        Assert.NotNull(approval);

        // 6. Transformation Execution in Isolated Working Copy
        var workingDir = Path.Combine(_tempRoot, "working-copy");
        CopyDirectory(extractDir, workingDir);

        var transformResults = await _dotNetWorker.ApplyTransformationsAsync(workingDir, plan);
        Assert.All(transformResults, r => Assert.True(r.Success, r.Message));

        // Check target Clean Architecture artifacts created
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.Domain/Project.Domain.csproj")));
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.Application/Project.Application.csproj")));
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.Infrastructure/Project.Infrastructure.csproj")));
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.API/Project.API.csproj")));
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.Domain/Entities/Order.cs")));
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.Application/Common/Interfaces/IOrderRepository.cs")));

        // The legacy project must be retired so it cannot double-compile.
        Assert.False(File.Exists(Path.Combine(workingDir, "LegacyShop.csproj")));

        // The extracted interface must mirror the real signatures, not a stub.
        var interfaceSource = File.ReadAllText(Path.Combine(workingDir, "src/Project.Application/Common/Interfaces/IOrderRepository.cs"));
        Assert.Contains("GetAllOrdersAsync", interfaceSource);
        Assert.Contains("GetOrderByIdAsync", interfaceSource);
        Assert.Contains("SaveOrderAsync", interfaceSource);
        Assert.DoesNotContain("Add contracts", interfaceSource);

        // The controller must depend on the abstraction, not the concrete repository.
        var controllerSource = File.ReadAllText(Path.Combine(workingDir, "src/Project.API/Controllers/OrdersController.cs"));
        Assert.Contains("IOrderRepository", controllerSource);
        Assert.DoesNotContain("private readonly OrderRepository ", controllerSource);
        Assert.Contains("Project.Application.Common.Interfaces", controllerSource);

        // A real composition root must have been synthesized.
        Assert.True(File.Exists(Path.Combine(workingDir, "src/Project.API/Program.cs")));

        // 7. Full validation pipeline: real build, tests, and architecture re-check.
        var report = await orchestrator.ExecuteValidationAsync(jobId, workingDir, LanguageKind.CSharp, transformResults);
        Assert.True(report.Build.ToolAvailable);
        Assert.True(report.Build.Success, report.Build.RawOutput);
        Assert.DoesNotContain(report.ArchitectureRules, r => r.RuleId == "ARCH001" && !r.Passed);
        Assert.DoesNotContain(report.ArchitectureRules, r => r.RuleId == "ARCH004" && !r.Passed);

        // Test reporting must be honest: a migrated solution with no test
        // projects cannot be reported as "tests passed". It must surface that no
        // tests were discovered instead of faking green.
        Assert.False(report.Tests.Success);
        Assert.Equal(0, report.Tests.TotalTests);
        Assert.Equal(ToolVerificationStatus.Failed, report.Tests.Status);
        Assert.Contains("cannot be reported as passing", report.Tests.RawOutput, StringComparison.OrdinalIgnoreCase);

        // Consequently the overall verdict is not a clean pass, but the failure
        // is the (disclosed) absence of tests, not a build or arch violation.
        Assert.False(report.OverallSuccess);
        Assert.Contains("No test projects were discovered", report.Tests.RawOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EndToEnd_JavaLayeredApp_CompletesAnalysisPlanAndTransformation()
    {
        var fixtureZipPath = GetFixtureZipPath("java-layered-app.zip");
        Assert.True(File.Exists(fixtureZipPath), $"Java fixture archive must exist at {fixtureZipPath}");

        var extractDir = Path.Combine(_tempRoot, "java-extracted");
        using var zipStream = File.OpenRead(fixtureZipPath);
        var extraction = await _extractor.ExtractSafelyAsync(zipStream, extractDir);

        Assert.True(extraction.Success);

        var jobId = Guid.NewGuid();
        var analysis = await _javaWorker.AnalyzeProjectAsync(extractDir, Guid.NewGuid(), jobId, extraction.Sha256);

        Assert.Equal(LanguageKind.Java, analysis.Language);
        Assert.Equal(BuildSystemKind.Maven, analysis.BuildSystem);
        Assert.Contains(analysis.Symbols, s => s.Name == "ProductController" && s.DetectedRole == SymbolRole.Controller);
        Assert.Contains(analysis.Symbols, s => s.Name == "Product" && s.DetectedRole == SymbolRole.Entity);
        Assert.Contains(analysis.Symbols, s => s.Name == "ProductRepository" && s.DetectedRole == SymbolRole.Repository);

        var plan = await _planner.GenerateMigrationPlanAsync(analysis);
        Assert.NotNull(plan);
        Assert.Equal(TargetArchitecture.JavaCleanArchitecture, plan.TargetArchitecture);
        Assert.Contains(plan.Operations, o => o.Type == OperationType.RewriteReferences);
        Assert.Contains(plan.Operations, o => o.Type == OperationType.FlagForManualRefactoring);

        var workingDir = Path.Combine(_tempRoot, "java-working");
        CopyDirectory(extractDir, workingDir);

        var transformResults = await _javaWorker.ApplyTransformationsAsync(workingDir, plan);
        Assert.All(transformResults, r => Assert.True(r.Success, r.Message));
        Assert.True(File.Exists(Path.Combine(workingDir, "domain/pom.xml")));
        Assert.True(File.Exists(Path.Combine(workingDir, "application/pom.xml")));
        Assert.True(File.Exists(Path.Combine(workingDir, "infrastructure/pom.xml")));
        Assert.True(File.Exists(Path.Combine(workingDir, "api/pom.xml")));

        // Sources must be relocated into the module layout...
        var controllerPath = Path.Combine(workingDir, "api/src/main/java/com/example/api/ProductController.java");
        var servicePath = Path.Combine(workingDir, "application/src/main/java/com/example/application/ProductService.java");
        Assert.True(File.Exists(controllerPath));
        Assert.True(File.Exists(servicePath));

        // ...with real package and import rewrites, not just file moves.
        var controllerSource = File.ReadAllText(controllerPath);
        Assert.Contains("package com.example.api;", controllerSource);
        Assert.Contains("import com.example.application.ProductService;", controllerSource);
        Assert.Contains("import com.example.domain.Product;", controllerSource);

        var serviceSource = File.ReadAllText(servicePath);
        Assert.Contains("package com.example.application;", serviceSource);
        Assert.Contains("import com.example.domain.Product;", serviceSource);

        // The legacy single-module pom is replaced by a reactor aggregator.
        var rootPom = File.ReadAllText(Path.Combine(workingDir, "pom.xml"));
        Assert.Contains("<packaging>pom</packaging>", rootPom);
        Assert.Contains("<module>domain</module>", rootPom);

        // Build/test validation must never fake success when the toolchain is absent.
        var build = await _javaWorker.BuildAsync(workingDir);
        Assert.DoesNotContain("Maven build verification completed", build.RawOutput);
        var tests = await _javaWorker.RunTestsAsync(workingDir);
        Assert.DoesNotContain("Java unit tests verified.", tests.RawOutput);
        if (!build.ToolAvailable)
        {
            Assert.Equal(ToolVerificationStatus.ToolUnavailable, build.Status);
            Assert.False(build.Success);
        }
        if (!tests.ToolAvailable)
        {
            Assert.Equal(ToolVerificationStatus.ToolUnavailable, tests.Status);
            Assert.False(tests.Success);
        }
    }

    [Fact]
    public async Task AnalyzeProject_DuplicateFullNamesFromPartialClasses_DoesNotThrow()
    {
        // Reproduces the real-world failure: an EF Core migration and its generated
        // .Designer.cs both declare `server.Migrations.AddUserAuthentication`.
        var projDir = Path.Combine(_tempRoot, "partials");
        var migrationsDir = Path.Combine(projDir, "Migrations");
        Directory.CreateDirectory(migrationsDir);

        File.WriteAllText(
            Path.Combine(migrationsDir, "AddUserAuthentication.cs"),
            "namespace server.Migrations;\n\npublic partial class AddUserAuthentication\n{\n    public void Up() { }\n}\n");

        File.WriteAllText(
            Path.Combine(migrationsDir, "AddUserAuthentication.Designer.cs"),
            "namespace server.Migrations;\n\npublic partial class AddUserAuthentication\n{\n    public void Down() { }\n}\n");

        var analysis = await _dotNetWorker.AnalyzeProjectAsync(projDir, Guid.NewGuid(), Guid.NewGuid(), "hash");

        // Partial classes sprawled across files are kept per-file (an EF migration
        // plus its .Designer.cs). Keeping one symbol per file guarantees every
        // file is relocated during transformation instead of one being dropped.
        var matchingSymbols = analysis.Symbols.Where(s => s.FullName == "server.Migrations.AddUserAuthentication").ToList();
        Assert.Equal(2, matchingSymbols.Count);
        Assert.Equal(2, analysis.DependencyGraph.Nodes.Count(n => n.Id == "server.Migrations.AddUserAuthentication"));

        // Every declaration file is represented and preserves its own members.
        var main = Assert.Single(matchingSymbols, s => s.RelativeFilePath.EndsWith("AddUserAuthentication.cs", StringComparison.Ordinal));
        Assert.Contains("Up", main.Methods);
        var designer = Assert.Single(matchingSymbols, s => s.RelativeFilePath.EndsWith("AddUserAuthentication.Designer.cs", StringComparison.Ordinal));
        Assert.Contains("Down", designer.Methods);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }
}

using System.Text.Json;
using CleanArchitectureMigration.ArchitectureRules;
using CleanArchitectureMigration.Contracts;
using CleanArchitectureMigration.DotNetWorker;
using CleanArchitectureMigration.JavaWorker;
using CleanArchitectureMigration.Orchestrator.AI;

namespace CleanArchitectureMigration.Orchestrator;

public interface IMigrationOrchestrator
{
    Task<ProjectAnalysisResult> ExecuteAnalysisAsync(
        Guid jobId,
        string sourceDirectory,
        Guid projectId,
        string sourceHash,
        CancellationToken cancellationToken = default);

    Task<MigrationPlan> ExecutePlanningAsync(
        Guid jobId,
        ProjectAnalysisResult analysis,
        string? customInstructions = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalRecord> VerifyAndRecordApprovalAsync(
        Guid jobId,
        MigrationPlan latestPlan,
        string expectedSourceHash,
        ApprovePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<List<TransformationOperationResult>> ExecuteTransformationsAsync(
        Guid jobId,
        string workingDirectory,
        MigrationPlan plan,
        LanguageKind language,
        CancellationToken cancellationToken = default);

    Task<ValidationReport> ExecuteValidationAsync(
        Guid jobId,
        string workingDirectory,
        LanguageKind language,
        List<TransformationOperationResult> transformationResults,
        CancellationToken cancellationToken = default);
}

public sealed class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly IDotNetWorker _dotNetWorker;
    private readonly IJavaWorker _javaWorker;
    private readonly ILLMService _llmService;
    private readonly ArchitectureRulesEngine _rulesEngine;

    public MigrationOrchestrator(
        IDotNetWorker dotNetWorker,
        IJavaWorker javaWorker,
        ILLMService llmService,
        ArchitectureRulesEngine rulesEngine)
    {
        _dotNetWorker = dotNetWorker;
        _javaWorker = javaWorker;
        _llmService = llmService;
        _rulesEngine = rulesEngine;
    }

    public async Task<ProjectAnalysisResult> ExecuteAnalysisAsync(
        Guid jobId,
        string sourceDirectory,
        Guid projectId,
        string sourceHash,
        CancellationToken cancellationToken = default)
    {
        // Auto-detect language
        var hasCsharp = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories).Any() ||
                        Directory.GetFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories).Any() ||
                        Directory.GetFiles(sourceDirectory, "*.sln", SearchOption.AllDirectories).Any();

        var hasJava = Directory.GetFiles(sourceDirectory, "*.java", SearchOption.AllDirectories).Any() ||
                      Directory.GetFiles(sourceDirectory, "pom.xml", SearchOption.AllDirectories).Any() ||
                      Directory.GetFiles(sourceDirectory, "build.gradle*", SearchOption.AllDirectories).Any();

        ProjectAnalysisResult analysis;

        if (hasCsharp)
        {
            analysis = await _dotNetWorker.AnalyzeProjectAsync(sourceDirectory, projectId, jobId, sourceHash, cancellationToken);
        }
        else if (hasJava)
        {
            analysis = await _javaWorker.AnalyzeProjectAsync(sourceDirectory, projectId, jobId, sourceHash, cancellationToken);
        }
        else
        {
            // Default to C# analysis if undetermined
            analysis = await _dotNetWorker.AnalyzeProjectAsync(sourceDirectory, projectId, jobId, sourceHash, cancellationToken);
        }

        // Run Clean Architecture rules engine on discovered facts
        _rulesEngine.EvaluateAll(analysis);

        return analysis;
    }

    public async Task<MigrationPlan> ExecutePlanningAsync(
        Guid jobId,
        ProjectAnalysisResult analysis,
        string? customInstructions = null,
        CancellationToken cancellationToken = default)
    {
        return await _llmService.GenerateMigrationPlanAsync(analysis, customInstructions, cancellationToken);
    }

    public Task<ApprovalRecord> VerifyAndRecordApprovalAsync(
        Guid jobId,
        MigrationPlan latestPlan,
        string expectedSourceHash,
        ApprovePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        // Cryptographic integrity check: Reject stale approval if source hash or plan version mismatch
        if (!string.Equals(request.SourceHash, expectedSourceHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Stale approval rejected: Request source hash '{request.SourceHash}' does not match active job source hash '{expectedSourceHash}'.");
        }

        if (request.PlanVersion != latestPlan.PlanVersion)
        {
            throw new InvalidOperationException($"Stale approval rejected: Plan version mismatch. Active plan version is {latestPlan.PlanVersion}, received {request.PlanVersion}.");
        }

        var approval = new ApprovalRecord
        {
            ApprovalId = Guid.NewGuid(),
            PlanId = latestPlan.PlanId,
            JobId = jobId,
            ApprovedBy = string.IsNullOrWhiteSpace(request.ApprovedBy) ? "developer" : request.ApprovedBy,
            SourceHash = request.SourceHash,
            PlanVersion = request.PlanVersion,
            ApprovedAtUtc = DateTime.UtcNow,
            Notes = request.Notes
        };

        return Task.FromResult(approval);
    }

    public async Task<List<TransformationOperationResult>> ExecuteTransformationsAsync(
        Guid jobId,
        string workingDirectory,
        MigrationPlan plan,
        LanguageKind language,
        CancellationToken cancellationToken = default)
    {
        if (language == LanguageKind.CSharp)
        {
            return await _dotNetWorker.ApplyTransformationsAsync(workingDirectory, plan, cancellationToken);
        }
        else
        {
            return await _javaWorker.ApplyTransformationsAsync(workingDirectory, plan, cancellationToken);
        }
    }

    public async Task<ValidationReport> ExecuteValidationAsync(
        Guid jobId,
        string workingDirectory,
        LanguageKind language,
        List<TransformationOperationResult> transformationResults,
        CancellationToken cancellationToken = default)
    {
        BuildResult buildResult;
        TestResult testResult;

        if (language == LanguageKind.CSharp)
        {
            buildResult = await _dotNetWorker.BuildAsync(workingDirectory, cancellationToken);
            testResult = await _dotNetWorker.RunTestsAsync(workingDirectory, cancellationToken);
        }
        else
        {
            buildResult = await _javaWorker.BuildAsync(workingDirectory, cancellationToken);
            testResult = await _javaWorker.RunTestsAsync(workingDirectory, cancellationToken);
        }

        // Re-analyze working directory to verify architecture rules after migration
        var postAnalysis = language == LanguageKind.CSharp
            ? await _dotNetWorker.AnalyzeProjectAsync(workingDirectory, Guid.Empty, jobId, "migrated", cancellationToken)
            : await _javaWorker.AnalyzeProjectAsync(workingDirectory, Guid.Empty, jobId, "migrated", cancellationToken);

        var ruleResults = _rulesEngine.EvaluateAll(postAnalysis);

        var overallSuccess = buildResult.Success && testResult.Success && ruleResults.All(r => r.Passed || r.Severity != ViolationSeverity.Critical);

        return new ValidationReport
        {
            JobId = jobId,
            OverallSuccess = overallSuccess,
            ValidatedAtUtc = DateTime.UtcNow,
            Build = buildResult,
            Tests = testResult,
            ArchitectureRules = ruleResults,
            TransformationResults = transformationResults,
            Summary = overallSuccess
                ? "Clean Architecture migration succeeded with clean build, tests passed, and architectural compliance verified."
                : "Migration completed with warnings or validation issues. Check detailed results."
        };
    }
}

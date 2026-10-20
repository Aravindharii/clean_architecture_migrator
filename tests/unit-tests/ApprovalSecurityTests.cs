using CleanArchitectureMigration.ArchitectureRules;
using CleanArchitectureMigration.Contracts;
using CleanArchitectureMigration.DotNetWorker;
using CleanArchitectureMigration.JavaWorker;
using CleanArchitectureMigration.Orchestrator;
using CleanArchitectureMigration.Orchestrator.AI;
using Xunit;

namespace CleanArchitectureMigration.Tests;

public class ApprovalSecurityTests
{
    private readonly MigrationOrchestrator _orchestrator;

    public ApprovalSecurityTests()
    {
        _orchestrator = new MigrationOrchestrator(
            new DotNetWorker.DotNetWorker(),
            new JavaWorker.JavaWorker(),
            new RuleAssistedPlannerService(),
            new ArchitectureRulesEngine()
        );
    }

    [Fact]
    public async Task VerifyAndRecordApprovalAsync_MatchingHashAndVersion_Succeeds()
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid(),
            PlanVersion = 2,
            SourceHash = "abc123hash"
        };

        var request = new ApprovePlanRequest
        {
            SourceHash = "abc123hash",
            PlanVersion = 2,
            ApprovedBy = "lead_architect",
            Notes = "Approved after review"
        };

        var result = await _orchestrator.VerifyAndRecordApprovalAsync(Guid.NewGuid(), plan, "abc123hash", request);

        Assert.NotNull(result);
        Assert.Equal("abc123hash", result.SourceHash);
        Assert.Equal(2, result.PlanVersion);
        Assert.Equal("lead_architect", result.ApprovedBy);
    }

    [Fact]
    public async Task VerifyAndRecordApprovalAsync_MismatchedSourceHash_RejectsStaleApproval()
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid(),
            PlanVersion = 1,
            SourceHash = "newSourceHash999"
        };

        var request = new ApprovePlanRequest
        {
            SourceHash = "oldSourceHash111", // Stale source hash
            PlanVersion = 1,
            ApprovedBy = "developer"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orchestrator.VerifyAndRecordApprovalAsync(Guid.NewGuid(), plan, "newSourceHash999", request));

        Assert.Contains("Stale approval rejected", ex.Message);
    }

    [Fact]
    public async Task VerifyAndRecordApprovalAsync_MismatchedPlanVersion_RejectsStaleApproval()
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid(),
            PlanVersion = 3, // Plan was regenerated to version 3
            SourceHash = "validHash"
        };

        var request = new ApprovePlanRequest
        {
            SourceHash = "validHash",
            PlanVersion = 2, // Developer was reviewing stale version 2
            ApprovedBy = "developer"
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orchestrator.VerifyAndRecordApprovalAsync(Guid.NewGuid(), plan, "validHash", request));

        Assert.Contains("Plan version mismatch", ex.Message);
    }
}

using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.Orchestrator.AI;

public interface ILLMService
{
    Task<MigrationPlan> GenerateMigrationPlanAsync(
        ProjectAnalysisResult analysis,
        string? customInstructions = null,
        CancellationToken cancellationToken = default);
}

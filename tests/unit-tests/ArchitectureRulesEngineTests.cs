using CleanArchitectureMigration.ArchitectureRules;
using CleanArchitectureMigration.Contracts;
using Xunit;

namespace CleanArchitectureMigration.Tests;

public class ArchitectureRulesEngineTests
{
    private readonly ArchitectureRulesEngine _engine = new();

    [Fact]
    public void EvaluateAll_PresentationDirectlyCallsInfrastructure_FlagsViolation()
    {
        var analysis = new ProjectAnalysisResult();

        var controller = new SymbolInfo
        {
            Name = "OrdersController",
            FullName = "Controllers.OrdersController",
            DetectedRole = SymbolRole.Controller,
            CurrentLayer = CleanArchitectureLayer.Presentation,
            Dependencies = ["Data.OrderRepository"]
        };

        var repo = new SymbolInfo
        {
            Name = "OrderRepository",
            FullName = "Data.OrderRepository",
            DetectedRole = SymbolRole.Repository,
            CurrentLayer = CleanArchitectureLayer.Infrastructure
        };

        analysis.Symbols.AddRange([controller, repo]);
        analysis.DependencyGraph.Nodes.Add(new GraphNode { Id = controller.FullName, Layer = "Presentation" });
        analysis.DependencyGraph.Nodes.Add(new GraphNode { Id = repo.FullName, Layer = "Infrastructure" });
        analysis.DependencyGraph.Edges.Add(new GraphEdge { Source = controller.FullName, Target = repo.FullName });

        var results = _engine.EvaluateAll(analysis);

        var arch001 = results.FirstOrDefault(r => r.RuleId == "ARCH001");
        Assert.NotNull(arch001);
        Assert.False(arch001.Passed);
        Assert.Contains(analysis.Violations, v => v.RuleId == "ARCH001");
    }

    [Fact]
    public void EvaluateAll_CircularDependencies_FlagsCycle()
    {
        var analysis = new ProjectAnalysisResult();

        analysis.DependencyGraph.Nodes.Add(new GraphNode { Id = "ComponentA" });
        analysis.DependencyGraph.Nodes.Add(new GraphNode { Id = "ComponentB" });
        analysis.DependencyGraph.Edges.Add(new GraphEdge { Source = "ComponentA", Target = "ComponentB" });
        analysis.DependencyGraph.Edges.Add(new GraphEdge { Source = "ComponentB", Target = "ComponentA" });

        var results = _engine.EvaluateAll(analysis);

        var arch006 = results.FirstOrDefault(r => r.RuleId == "ARCH006");
        Assert.NotNull(arch006);
        Assert.False(arch006.Passed);
        Assert.Contains("ComponentA -> ComponentB -> ComponentA", arch006.OffendingSymbols);
    }

    [Fact]
    public void EvaluateAll_PureCleanArchitectureLayers_PassesAllRules()
    {
        var analysis = new ProjectAnalysisResult();

        var entity = new SymbolInfo
        {
            Name = "Order",
            FullName = "Project.Domain.Entities.Order",
            DetectedRole = SymbolRole.Entity,
            CurrentLayer = CleanArchitectureLayer.Domain
        };

        var useCase = new SymbolInfo
        {
            Name = "CreateOrderUseCase",
            FullName = "Project.Application.UseCases.CreateOrderUseCase",
            DetectedRole = SymbolRole.Service,
            CurrentLayer = CleanArchitectureLayer.Application,
            Dependencies = [entity.FullName, "Project.Application.Common.Interfaces.IOrderRepository"]
        };

        var repoInterface = new SymbolInfo
        {
            Name = "IOrderRepository",
            FullName = "Project.Application.Common.Interfaces.IOrderRepository",
            DetectedRole = SymbolRole.Interface,
            CurrentLayer = CleanArchitectureLayer.Application
        };

        analysis.Symbols.AddRange([entity, useCase, repoInterface]);

        var results = _engine.EvaluateAll(analysis);

        Assert.All(results, r => Assert.True(r.Passed));
        Assert.Empty(analysis.Violations);
    }
}

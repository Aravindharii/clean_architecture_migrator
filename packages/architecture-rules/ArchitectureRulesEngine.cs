using CleanArchitectureMigration.Contracts;

namespace CleanArchitectureMigration.ArchitectureRules;

public interface IArchitectureRule
{
    string RuleId { get; }
    string Name { get; }
    string Description { get; }
    ViolationSeverity DefaultSeverity { get; }
    RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis);
}

public sealed class ArchitectureRulesEngine
{
    private readonly List<IArchitectureRule> _rules;

    public ArchitectureRulesEngine()
    {
        _rules =
        [
            new PresentationMustNotDependOnInfrastructureRule(),
            new DomainMustNotDependOnOuterLayersRule(),
            new ApplicationMustNotDependOnInfrastructureConcreteRule(),
            new ControllersMustNotDirectlyAccessRepositoriesRule(),
            new NoFrameworkLeakageIntoDomainRule(),
            new NoCircularDependenciesRule()
        ];
    }

    public List<RuleEvaluationResult> EvaluateAll(ProjectAnalysisResult analysis)
    {
        var results = new List<RuleEvaluationResult>();
        analysis.Violations.Clear();

        foreach (var rule in _rules)
        {
            var res = rule.Evaluate(analysis);
            results.Add(res);

            if (!res.Passed)
            {
                foreach (var offending in res.OffendingSymbols)
                {
                    analysis.Violations.Add(new ArchitectureViolation
                    {
                        RuleId = rule.RuleId,
                        Severity = res.Severity,
                        SourceSymbol = offending,
                        TargetSymbol = res.RuleName,
                        Description = res.Message,
                        Recommendation = $"Refactor {offending} to conform with Clean Architecture boundaries."
                    });
                }
            }
        }

        // Also update edges in dependency graph with violation flags
        foreach (var edge in analysis.DependencyGraph.Edges)
        {
            var sourceSym = analysis.Symbols.FirstOrDefault(s => s.FullName == edge.Source || s.Name == edge.Source);
            var targetSym = analysis.Symbols.FirstOrDefault(s => s.FullName == edge.Target || s.Name == edge.Target);

            if (sourceSym != null && targetSym != null)
            {
                if (sourceSym.CurrentLayer == CleanArchitectureLayer.Presentation &&
                    targetSym.CurrentLayer == CleanArchitectureLayer.Infrastructure)
                {
                    edge.IsViolation = true;
                    edge.ViolationReason = "Presentation layer must not directly depend on Infrastructure.";
                }
                else if (sourceSym.CurrentLayer == CleanArchitectureLayer.Domain &&
                         (targetSym.CurrentLayer == CleanArchitectureLayer.Infrastructure ||
                          targetSym.CurrentLayer == CleanArchitectureLayer.Application ||
                          targetSym.CurrentLayer == CleanArchitectureLayer.Presentation))
                {
                    edge.IsViolation = true;
                    edge.ViolationReason = "Domain layer must not depend on outer layers.";
                }
                else if (sourceSym.DetectedRole == SymbolRole.Controller &&
                         targetSym.DetectedRole == SymbolRole.Repository)
                {
                    edge.IsViolation = true;
                    edge.ViolationReason = "Controllers must not directly access Repositories (bypass of Application layer).";
                }
            }
        }

        return results;
    }
}

public sealed class PresentationMustNotDependOnInfrastructureRule : IArchitectureRule
{
    public string RuleId => "ARCH001";
    public string Name => "Presentation Must Not Depend On Infrastructure";
    public string Description => "The presentation/API layer should only depend on Application and Domain abstractions, never concrete infrastructure.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Critical;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var offending = new List<string>();

        foreach (var sym in analysis.Symbols.Where(s => s.CurrentLayer == CleanArchitectureLayer.Presentation || s.DetectedRole == SymbolRole.Controller))
        {
            foreach (var depName in sym.Dependencies)
            {
                var target = analysis.Symbols.FirstOrDefault(s => s.FullName == depName || s.Name == depName);
                if (target != null && target.CurrentLayer == CleanArchitectureLayer.Infrastructure)
                {
                    offending.Add($"{sym.FullName} -> {target.FullName}");
                }
            }
        }

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = offending.Count == 0,
            Severity = DefaultSeverity,
            Message = offending.Count == 0
                ? "No direct Presentation -> Infrastructure dependencies found."
                : $"Found {offending.Count} illegal direct Presentation -> Infrastructure dependency links.",
            OffendingSymbols = offending
        };
    }
}

public sealed class DomainMustNotDependOnOuterLayersRule : IArchitectureRule
{
    public string RuleId => "ARCH002";
    public string Name => "Domain Must Not Depend On Outer Layers";
    public string Description => "Domain core must have zero outward dependencies onto Application, Infrastructure, or Presentation.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Critical;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var offending = new List<string>();

        foreach (var sym in analysis.Symbols.Where(s => s.CurrentLayer == CleanArchitectureLayer.Domain || s.DetectedRole == SymbolRole.Entity))
        {
            foreach (var depName in sym.Dependencies)
            {
                var target = analysis.Symbols.FirstOrDefault(s => s.FullName == depName || s.Name == depName);
                if (target != null && (target.CurrentLayer == CleanArchitectureLayer.Infrastructure ||
                                       target.CurrentLayer == CleanArchitectureLayer.Presentation ||
                                       target.CurrentLayer == CleanArchitectureLayer.Application))
                {
                    offending.Add($"{sym.FullName} -> {target.FullName} ({target.CurrentLayer})");
                }
            }
        }

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = offending.Count == 0,
            Severity = DefaultSeverity,
            Message = offending.Count == 0
                ? "Domain layer is pure with no outward dependencies."
                : $"Found {offending.Count} outward dependency violations from Domain into outer layers.",
            OffendingSymbols = offending
        };
    }
}

public sealed class ApplicationMustNotDependOnInfrastructureConcreteRule : IArchitectureRule
{
    public string RuleId => "ARCH003";
    public string Name => "Application Must Not Depend On Concrete Infrastructure";
    public string Description => "Application use cases and handlers must depend on repository and external service abstractions, not concrete DB contexts or drivers.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Major;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var offending = new List<string>();

        // Only symbols that actually live in the Application layer are subject
        // to this rule. Concrete services intentionally hosted in
        // Infrastructure (e.g. a persistence adapter service that takes
        // AppDbContext) legitimately reference the DbContext and are not
        // violations; including symbols merely by their "Service" role flags
        // those false positives after a migration relocates them.
        foreach (var sym in analysis.Symbols.Where(s => s.CurrentLayer == CleanArchitectureLayer.Application))
        {
            foreach (var depName in sym.Dependencies)
            {
                var target = analysis.Symbols.FirstOrDefault(s => s.FullName == depName || s.Name == depName);
                if (target != null && target.CurrentLayer == CleanArchitectureLayer.Infrastructure && target.Kind != "interface")
                {
                    offending.Add($"{sym.FullName} -> {target.FullName}");
                }
            }
        }

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = offending.Count == 0,
            Severity = DefaultSeverity,
            Message = offending.Count == 0
                ? "Application layer correctly depends on abstractions."
                : $"Found {offending.Count} concrete infrastructure references in the Application layer.",
            OffendingSymbols = offending
        };
    }
}

public sealed class ControllersMustNotDirectlyAccessRepositoriesRule : IArchitectureRule
{
    public string RuleId => "ARCH004";
    public string Name => "Controllers Must Not Directly Access Repositories";
    public string Description => "API controllers must delegate business operations to Application services or Use Cases rather than directly accessing Repositories or DbContext.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Major;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var offending = new List<string>();

        foreach (var sym in analysis.Symbols.Where(s => s.DetectedRole == SymbolRole.Controller))
        {
            foreach (var dep in sym.Dependencies)
            {
                var target = analysis.Symbols.FirstOrDefault(s => s.FullName == dep || s.Name == dep);
                if (target == null) continue;

                // Only concrete data-access types are violations. Application-layer
                // repository interfaces (IOrderRepository) are the correct
                // abstraction and must not be flagged.
                var isConcreteDataAccess =
                    target.DetectedRole == SymbolRole.Repository ||
                    target.DetectedRole == SymbolRole.Configuration ||
                    (!string.Equals(target.Kind, "interface", StringComparison.OrdinalIgnoreCase) &&
                     target.Name.Contains("DbContext", StringComparison.Ordinal));

                if (isConcreteDataAccess)
                {
                    offending.Add($"{sym.FullName} -> {target.FullName}");
                }
            }
        }

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = offending.Count == 0,
            Severity = DefaultSeverity,
            Message = offending.Count == 0
                ? "No direct controller-to-repository couplings found."
                : $"Found {offending.Count} controllers directly accessing repositories or database contexts.",
            OffendingSymbols = offending
        };
    }
}

public sealed class NoFrameworkLeakageIntoDomainRule : IArchitectureRule
{
    public string RuleId => "ARCH005";
    public string Name => "No Framework Leakage Into Domain";
    public string Description => "Domain models must not contain web/API attributes, ORM-specific base classes, or framework annotations.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Minor;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var offending = new List<string>();

        foreach (var sym in analysis.Symbols.Where(s => s.CurrentLayer == CleanArchitectureLayer.Domain || s.DetectedRole == SymbolRole.Entity))
        {
            if (sym.HasFrameworkLeakage || sym.AttributesOrAnnotations.Any(a => 
                a.Contains("JsonProperty") || a.Contains("Table") || a.Contains("Column") || a.Contains("Entity") || a.Contains("Table") || a.Contains("ForeignKey")))
            {
                offending.Add($"{sym.FullName} has framework annotations: [{string.Join(", ", sym.AttributesOrAnnotations)}]");
            }
        }

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = offending.Count == 0,
            Severity = DefaultSeverity,
            Message = offending.Count == 0
                ? "Domain entities have clean POCO/POJO design without framework leakage."
                : $"Found {offending.Count} domain symbols with framework annotations or attributes.",
            OffendingSymbols = offending
        };
    }
}

public sealed class NoCircularDependenciesRule : IArchitectureRule
{
    public string RuleId => "ARCH006";
    public string Name => "No Circular Dependencies";
    public string Description => "Dependency graph must be a Directed Acyclic Graph (DAG) with no circular dependencies.";
    public ViolationSeverity DefaultSeverity => ViolationSeverity.Critical;

    public RuleEvaluationResult Evaluate(ProjectAnalysisResult analysis)
    {
        var cycles = FindCycles(analysis.DependencyGraph);

        return new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleName = Name,
            Passed = cycles.Count == 0,
            Severity = DefaultSeverity,
            Message = cycles.Count == 0
                ? "No circular dependencies detected."
                : $"Detected {cycles.Count} circular dependency cycle(s).",
            OffendingSymbols = cycles
        };
    }

    private static List<string> FindCycles(DependencyGraphData graph)
    {
        var adj = new Dictionary<string, List<string>>();
        foreach (var edge in graph.Edges)
        {
            if (!adj.ContainsKey(edge.Source))
                adj[edge.Source] = [];
            adj[edge.Source].Add(edge.Target);
        }

        var visited = new HashSet<string>();
        var recStack = new HashSet<string>();
        var cycles = new List<string>();

        foreach (var node in graph.Nodes)
        {
            CheckCycleDfs(node.Id, adj, visited, recStack, new List<string>(), cycles);
        }

        return cycles.Distinct().ToList();
    }

    private static bool CheckCycleDfs(string current, Dictionary<string, List<string>> adj,
        HashSet<string> visited, HashSet<string> recStack, List<string> path, List<string> cycles)
    {
        if (recStack.Contains(current))
        {
            var cyclePath = string.Join(" -> ", path.SkipWhile(p => p != current).Concat([current]));
            cycles.Add(cyclePath);
            return true;
        }

        if (visited.Contains(current))
            return false;

        visited.Add(current);
        recStack.Add(current);
        path.Add(current);

        if (adj.TryGetValue(current, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                CheckCycleDfs(neighbor, adj, visited, recStack, path, cycles);
            }
        }

        path.RemoveAt(path.Count - 1);
        recStack.Remove(current);
        return false;
    }
}

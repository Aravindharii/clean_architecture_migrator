using System.Text.Json.Serialization;

namespace CleanArchitectureMigration.Contracts;

public sealed class ProjectAnalysisResult
{
    public Guid ProjectId { get; set; }
    public Guid JobId { get; set; }
    public string SourceHash { get; set; } = string.Empty;
    public LanguageKind Language { get; set; } = LanguageKind.Unknown;
    public string Framework { get; set; } = string.Empty;
    public BuildSystemKind BuildSystem { get; set; } = BuildSystemKind.Unknown;
    public string DetectedRootPath { get; set; } = string.Empty;
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ModuleInfo> Modules { get; set; } = [];
    public List<SourceFileInfo> SourceFiles { get; set; } = [];
    public List<SymbolInfo> Symbols { get; set; } = [];
    public DependencyGraphData DependencyGraph { get; set; } = new();
    public List<ArchitectureViolation> Violations { get; set; } = [];
    public List<DatabaseDependencyInfo> DatabaseDependencies { get; set; } = [];
    public List<ExternalServiceDependencyInfo> ExternalIntegrations { get; set; } = [];
    public List<string> UnsupportedConstructs { get; set; } = [];
    public BuildStatus PreExistingBuildStatus { get; set; } = new();
}

public sealed class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public List<string> ProjectReferences { get; set; } = [];
    public List<string> PackageDependencies { get; set; } = [];
    public bool IsTestModule { get; set; }
}

public sealed class SourceFileInfo
{
    public string RelativePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public int LineCount { get; set; }
}

public sealed class SymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Kind { get; set; } = "class"; // class, interface, record, enum
    public string RelativeFilePath { get; set; } = string.Empty;
    public string NamespaceOrPackage { get; set; } = string.Empty;
    public SymbolRole DetectedRole { get; set; } = SymbolRole.Unknown;
    public CleanArchitectureLayer CurrentLayer { get; set; } = CleanArchitectureLayer.Unknown;
    public CleanArchitectureLayer TargetCleanLayer { get; set; } = CleanArchitectureLayer.Unknown;
    // Fully-qualified namespace this symbol should live in after migration. Empty
    // when the symbol cannot be classified and requires manual review.
    public string TargetNamespace { get; set; } = string.Empty;
    public double RoleConfidence { get; set; } = 1.0;
    public List<string> RoleEvidence { get; set; } = [];
    public List<string> AttributesOrAnnotations { get; set; } = [];
    public List<string> InterfacesImplemented { get; set; } = [];
    public string? BaseClass { get; set; }
    public List<string> Dependencies { get; set; } = [];
    public List<string> Methods { get; set; } = [];
    public bool HasFrameworkLeakage { get; set; }
}

public sealed class DependencyGraphData
{
    public List<GraphNode> Nodes { get; set; } = [];
    public List<GraphEdge> Edges { get; set; } = [];
}

public sealed class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Layer { get; set; } = string.Empty; // Presentation, Application, Domain, Infrastructure
    public string Role { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
}

public sealed class GraphEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DependencyType Type { get; set; } = DependencyType.References;
    public bool IsViolation { get; set; }
    public string? ViolationReason { get; set; }
}

public sealed class ArchitectureViolation
{
    public string RuleId { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Major;
    public string SourceSymbol { get; set; } = string.Empty;
    public string TargetSymbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
}

public sealed class DatabaseDependencyInfo
{
    public string Technology { get; set; } = string.Empty; // EF Core, Spring Data JPA, JDBC, Dapper, etc.
    public string DeclaredInSymbol { get; set; } = string.Empty;
    public string TableOrEntity { get; set; } = string.Empty;
}

public sealed class ExternalServiceDependencyInfo
{
    public string ServiceType { get; set; } = string.Empty; // HTTP, Messaging, Cloud SDK
    public string DeclaredInSymbol { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class BuildStatus
{
    public bool Success { get; set; } = true;
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public string OutputSummary { get; set; } = string.Empty;
}

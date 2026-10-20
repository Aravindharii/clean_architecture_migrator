namespace CleanArchitectureMigration.Contracts;

public enum JobStatus
{
    Uploaded,
    Extracted,
    Analyzing,
    AnalysisReady,
    Planning,
    AwaitingApproval,
    Approved,
    Transforming,
    Validating,
    Completed,
    Failed,
    Cancelled
}

public enum LanguageKind
{
    CSharp,
    Java,
    Unknown
}

public enum BuildSystemKind
{
    MSBuild,
    Maven,
    Gradle,
    Unknown
}

public enum TargetArchitecture
{
    DotNetCleanArchitecture,
    JavaCleanArchitecture
}

public enum SymbolRole
{
    Controller,
    Service,
    Repository,
    Entity,
    Dto,
    Interface,
    Configuration,
    ExternalIntegration,
    DomainService,
    DomainEvent,
    ValueObject,
    Middleware,
    Test,
    Unknown
}

public enum CleanArchitectureLayer
{
    Domain,
    Application,
    Infrastructure,
    Presentation,
    Shared,
    Unknown
}

public enum OperationType
{
    CreateProjectOrModule,
    MoveFile,
    RenameNamespaceOrPackage,
    UpdateProjectReferences,
    ExtractInterface,
    UpdateDependencyInjection,
    CreateApplicationUseCase,
    ProposeRepositoryAbstraction,
    RewriteReferences,
    RemoveLegacyProject,
    FlagForManualRefactoring,
    AddArchitectureValidationRules
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum ViolationSeverity
{
    Critical,
    Major,
    Minor,
    Info
}

public enum DependencyType
{
    Calls,
    Implements,
    Inherits,
    Injects,
    References,
    UsesType
}

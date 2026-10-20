export type JobStatus =
  | 'Uploaded'
  | 'Extracted'
  | 'Analyzing'
  | 'AnalysisReady'
  | 'Planning'
  | 'AwaitingApproval'
  | 'Approved'
  | 'Transforming'
  | 'Validating'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'

export type LanguageKind = 'CSharp' | 'Java' | 'Unknown'

export type BuildSystemKind = 'MSBuild' | 'Maven' | 'Gradle' | 'Unknown'

export type SymbolRole =
  | 'Controller'
  | 'Service'
  | 'Repository'
  | 'Entity'
  | 'Dto'
  | 'Interface'
  | 'Configuration'
  | 'ExternalIntegration'
  | 'DomainService'
  | 'DomainEvent'
  | 'ValueObject'
  | 'Middleware'
  | 'Test'
  | 'Unknown'

export type CleanArchitectureLayer =
  | 'Domain'
  | 'Application'
  | 'Infrastructure'
  | 'Presentation'
  | 'Shared'
  | 'Unknown'

export type OperationType =
  | 'CreateProjectOrModule'
  | 'MoveFile'
  | 'RenameNamespaceOrPackage'
  | 'UpdateProjectReferences'
  | 'ExtractInterface'
  | 'UpdateDependencyInjection'
  | 'CreateApplicationUseCase'
  | 'ProposeRepositoryAbstraction'
  | 'FlagForManualRefactoring'
  | 'AddArchitectureValidationRules'

export type RiskLevel = 'Low' | 'Medium' | 'High'
export type ViolationSeverity = 'Critical' | 'Major' | 'Minor' | 'Info'

export interface ProjectResponse {
  id: string
  name: string
  description?: string
  createdAtUtc: string
  latestJobId?: string
  latestJobStatus?: JobStatus
}

export interface JobStatusResponse {
  jobId: string
  projectId: string
  status: JobStatus
  sourceHash: string
  language: LanguageKind
  framework: string
  createdAtUtc: string
  completedAtUtc?: string
  currentStep?: string
  progressPercent: number
  errorMessage?: string
  planId?: string
  hasApproval: boolean
}

export interface SymbolInfo {
  name: string
  fullName: string
  kind: string
  relativeFilePath: string
  namespaceOrPackage: string
  detectedRole: SymbolRole
  currentLayer: CleanArchitectureLayer
  targetCleanLayer: CleanArchitectureLayer
  roleConfidence: number
  roleEvidence: string[]
  attributesOrAnnotations: string[]
  interfacesImplemented: string[]
  baseClass?: string
  dependencies: string[]
  methods: string[]
  hasFrameworkLeakage: boolean
}

export interface GraphNode {
  id: string
  label: string
  layer: string
  role: string
  filePath: string
  violationCount: number
}

export interface GraphEdge {
  source: string
  target: string
  type: string
  isViolation: boolean
  violationReason?: string
}

export interface DependencyGraphData {
  nodes: GraphNode[]
  edges: GraphEdge[]
}

export interface ArchitectureViolation {
  ruleId: string
  severity: ViolationSeverity
  sourceSymbol: string
  targetSymbol: string
  description: string
  recommendation: string
  sourceFilePath?: string
}

export interface ProjectAnalysisResult {
  projectId: string
  jobId: string
  sourceHash: string
  language: LanguageKind
  framework: string
  buildSystem: BuildSystemKind
  detectedRootPath: string
  analyzedAtUtc: string
  modules: any[]
  sourceFiles: any[]
  symbols: SymbolInfo[]
  dependencyGraph: DependencyGraphData
  violations: ArchitectureViolation[]
  databaseDependencies: any[]
  externalIntegrations: any[]
  unsupportedConstructs: string[]
}

export interface PlanOperation {
  operationId: string
  sequence: number
  type: OperationType
  description: string
  sourcePath?: string
  destinationPath?: string
  parameters: Record<string, string>
  reason: string
  evidence: string[]
  confidence: number
  risk: RiskLevel
  prerequisites: string[]
  requiresApproval: boolean
  isApproved: boolean
  manualReviewRequired: boolean
  codeDiffPreview?: string
}

export interface MigrationPlan {
  planId: string
  jobId: string
  planVersion: number
  sourceHash: string
  targetArchitecture: string
  summary: string
  overallRisk: RiskLevel
  estimatedEffortHours: number
  operations: PlanOperation[]
  createdAtUtc: string
  updatedAtUtc?: string
}

export interface ApprovalRecord {
  approvalId: string
  planId: string
  jobId: string
  approvedBy: string
  sourceHash: string
  planVersion: number
  approvedAtUtc: string
  notes: string
  isRevoked: boolean
}

export interface RuleEvaluationResult {
  ruleId: string
  ruleName: string
  passed: boolean
  severity: ViolationSeverity
  message: string
  offendingSymbols: string[]
}

export interface BuildResult {
  success: boolean
  errorsCount: number
  warningsCount: number
  rawOutput: string
}

export interface TestResult {
  success: boolean
  totalTests: number
  passedTests: number
  failedTests: number
  duration: string
  rawOutput: string
}

export interface TransformationOperationResult {
  operationId: string
  type: OperationType
  success: boolean
  affectedPath?: string
  message: string
  duration: string
}

export interface ValidationReport {
  jobId: string
  overallSuccess: boolean
  validatedAtUtc: string
  build: BuildResult
  tests: TestResult
  architectureRules: RuleEvaluationResult[]
  transformationResults: TransformationOperationResult[]
  summary: string
}

export interface ArtifactMetadata {
  artifactId: string
  jobId: string
  fileName: string
  contentType: string
  sizeBytes: number
  sha256: string
  createdAtUtc: string
  downloadUrl: string
}

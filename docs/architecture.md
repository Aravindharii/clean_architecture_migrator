# AI-Assisted Clean Architecture Migration Platform
## Architecture Specification

### 1. System Overview
The **AI-Assisted Clean Architecture Migration Platform** provides automated, deterministic, and safe migration of legacy layered .NET and Java applications into Clean Architecture solutions.

The platform coordinates analysis, AI-assisted migration planning, developer review with visual diffs, deterministic code transformations, and isolated build and test validation.

### 2. High-Level Component Architecture

```
                  ┌──────────────────────────────────────────────┐
                  │                 React Web App                │
                  │ (Vite, TS, Tailwind, ReactFlow/SVG, Monaco)  │
                  └──────────────────────┬───────────────────────┘
                                         │ REST / SSE
                                         ▼
                  ┌──────────────────────────────────────────────┐
                  │             ASP.NET Core Web API             │
                  │   - Uploads & Safe Archive Extraction        │
                  │   - Job State Machine (PostgreSQL)           │
                  │   - Provider-Neutral LLM Planner             │
                  │   - Approval Verification (Hash + Version)   │
                  │   - Artifact Storage & Export                │
                  └──────────────┬───────────────────────────────┘
                                 │ Background Dispatch (RabbitMQ / Channel)
                                 ▼
                  ┌──────────────────────────────────────────────┐
                  │            Migration Orchestrator            │
                  │       Durable State Machine Pipeline         │
                  └──────────────┬───────────────────────────────┘
                                 │
                 ┌───────────────┴───────────────┐
                 ▼                               ▼
  ┌─────────────────────────────┐ ┌─────────────────────────────┐
  │      .NET Worker            │ │        Java Worker          │
  │ - Roslyn Syntax & Semantic  │ │ - Java AST & Symbol Parser  │
  │ - MSBuild Project Analysis  │ │ - Maven / Gradle Parser     │
  │ - Deterministic Rewriter    │ │ - AST & Recipe Transformer  │
  │ - dotnet build & test       │ │ - Isolated Build Container  │
  └─────────────────────────────┘ └─────────────────────────────┘
```

### 3. Pipeline States
1. `Uploaded`: ZIP archive received, size & file limits verified.
2. `Extracted`: Safe extraction performed, entry paths normalized, SHA-256 hash computed.
3. `Analyzing`: Language-specific worker parses AST, symbols, dependencies, and architecture rules.
4. `AnalysisReady`: Language-neutral Intermediate Representation (IR) generated.
5. `Planning`: AI planner processes IR with secret redaction and generates structured JSON migration operations.
6. `AwaitingApproval`: Developer reviews before/after dependency graph, operation steps, and file diffs.
7. `Approved`: Developer cryptographic sign-off verified with exact source hash and plan version.
8. `Transforming`: Deterministic transformation engine moves files, rewrites namespaces/packages, extracts interfaces, updates DI and references.
9. `Validating`: Migrated solution compiled, tests executed, architectural constraints validated.
10. `Completed`: Final validation report generated and output ZIP artifact prepared for download.

* Failure states: `Failed`, `Cancelled`.

### 4. Security Principles
- Untrusted archive inspection: No zip slip, path traversal, symlink resolution, or zip bombs.
- Original source files are immutable (`readonly` sandbox copy used for transformation).
- AI has zero command execution privileges; only emits structured JSON adhering to strict JSON Schema.
- Redaction of sensitive strings (credentials, connection strings) prior to AI dispatch.
- Sandboxed build validation with resource limits.

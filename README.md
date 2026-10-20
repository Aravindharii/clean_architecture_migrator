# ArchMorph: AI-Assisted Clean Architecture Migration Platform

ArchMorph is a production-oriented developer platform that analyzes existing layered monolithic applications in **.NET and Java** and assists software architects and engineering teams in migrating them to **Clean Architecture**.

The platform combines **AI architectural recommendations**, **deterministic static code analysis and AST transformations** (Roslyn compiler APIs for C# and Java AST/package parsers), **cryptographic developer approval enforcement**, and **isolated sandbox build and test validation**.

---

## 1. System Architecture

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

---

## 2. Monorepo Structure

* `apps/web`: React, TypeScript, Vite, Tailwind CSS, TanStack Query frontend.
* `apps/api`: ASP.NET Core platform Web API with Entity Framework Core, PostgreSQL, and OpenAPI.
* `services/migration-orchestrator`: Durable state machine workflow and worker coordination.
* `services/dotnet-worker`: .NET analysis and transformation using Roslyn compiler APIs.
* `services/java-worker`: Java analysis and transformation for Spring Boot, Maven, and Gradle.
* `packages/migration-contracts`: Shared data contracts, DTOs, and JSON Schema definitions.
* `packages/architecture-rules`: Clean Architecture rules evaluation engine (`ARCH001` - `ARCH006`).
* `packages/migration-prompts`: Versioned AI prompt templates (`v1.0.0`).
* `templates/dotnet-clean-architecture`: Target .NET Clean Architecture solution templates (`.Domain`, `.Application`, `.Infrastructure`, `.API`).
* `templates/java-clean-architecture`: Target Java Clean Architecture project templates (`domain`, `application`, `infrastructure`, `api`).
* `tests/migration-fixtures`: Real layered .NET and Java fixture projects and pre-packaged ZIP archives.
* `tests/unit-tests`: Comprehensive automated unit and end-to-end integration test suite.
* `deploy/docker`: Dockerfiles and `docker-compose.yml` for PostgreSQL, RabbitMQ, API, and Web UI.

---

## 3. Target Clean Architecture Structures

### .NET Target Solution
* `Project.Domain`: Entities, value objects, domain events, domain exceptions (zero outward dependencies).
* `Project.Application`: Use cases, commands, queries, DTOs, repository and external service abstractions.
* `Project.Infrastructure`: EF Core DbContext, repository implementations, database migrations, external integrations.
* `Project.API`: Controllers, HTTP middleware, filters, and dependency injection composition root.

### Java Target (Spring Boot)
* `domain`: Pure POJO entities and domain rules.
* `application`: Use cases, DTOs, and repository ports.
* `infrastructure`: Spring Data JPA repositories, database entities, and persistence adapters.
* `api`: Spring Boot `@RestController` presentation endpoints.

---

## 4. Security Principles

1. **Untrusted Archive Inspection**: Protects against Zip Slip directory traversal (`..`), zip decompression bombs (file count & ratio limits), and symlink/hardlink exploits.
2. **Read-Only Source Archive**: The uploaded original ZIP file is preserved and never modified. Transformations are executed strictly within an isolated disposable working copy.
3. **Secret Redaction**: Credentials, connection string passwords, JWT tokens, and private keys are redacted (`[REDACTED]`) before static code facts reach the LLM planner.
4. **Zero Shell Privileges for AI**: The LLM emits structured JSON conforming to a strict schema; it is never allowed to execute shell commands or apply unrestricted edits.
5. **Cryptographically Bound Approval**: Execution requires explicit developer approval bound to `SHA-256(source)` and `PlanVersion`. If the source archive changes or the plan is regenerated, prior approvals are automatically revoked.

---

## 5. Getting Started

### Prerequisites
* [.NET 10 or .NET 8+ SDK](https://dotnet.microsoft.com/)
* [Node.js v20+ and npm](https://nodejs.org/)
* [Docker and Docker Compose](https://www.docker.com/) (optional for containerized deployment)

### Local Development

1. **Run Unit & Integration Tests**:
   ```bash
   dotnet test CleanArchitectureMigration.slnx
   ```

2. **Start the ASP.NET Core API**:
   ```bash
   dotnet run --project apps/api/CleanArchitectureMigration.Api.csproj --urls=http://localhost:5000
   ```
   * Swagger Documentation available at: `http://localhost:5000/swagger`

3. **Start the React Frontend**:
   ```bash
   cd apps/web
   npm install
   npm run dev
   ```
   * Access the UI at: `http://localhost:3000`

### Docker Compose Deployment
```bash
docker compose -f deploy/docker/docker-compose.yml up --build
```
Services:
* Web UI: `http://localhost:3000`
* API & Swagger: `http://localhost:5000/swagger`
* RabbitMQ Management: `http://localhost:15672` (guest / guest)
* PostgreSQL: `localhost:5432`

---

## 6. End-to-End Migration Workflow Walkthrough

1. **Upload Archive**: Upload `tests/migration-fixtures/dotnet-layered-app.zip` or `tests/migration-fixtures/java-layered-app.zip`.
2. **Static Code Analysis**: The platform computes the source SHA-256, parses AST symbols, builds a directed dependency graph, and detects architectural violations.
3. **AI Planning**: Generates structured, ordered migration operations (creating target projects, relocating files, renaming namespaces, extracting interfaces, updating DI registrations).
4. **Developer Review**: Review proposed changes, file code diffs, and confidence/risk levels. Toggle approval per operation or edit parameters.
5. **Developer Sign-Off**: Provide cryptographically bound approval matching the source hash and plan version.
6. **Execution & Validation**: The orchestrator creates an isolated working copy, applies deterministic transformations, compiles the project, runs tests, and re-evaluates architecture rules.
7. **Download Artifacts**: Download `migrated-clean-architecture.zip` and the complete audit report JSON.

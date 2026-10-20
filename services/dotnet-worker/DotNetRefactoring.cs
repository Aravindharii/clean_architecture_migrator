using CleanArchitectureMigration.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CleanArchitectureMigration.DotNetWorker;

/// <summary>
/// Describes a repository abstraction that must be extracted from a concrete
/// implementation and inverted into the Application layer.
/// </summary>
public sealed record RepositoryInterfaceSpec(
    string ImplementationClass,
    string ImplementationPath,
    string InterfaceName,
    string InterfaceNamespace,
    string DestinationPath,
    List<string> Usings);

/// <summary>
/// Provides deterministic, Roslyn-based semantic transformations for the
/// layered-to-clean-architecture migration. All rewrites operate on syntax
/// trees (namespace declarations, using directives, identifiers and base
/// lists) rather than on raw string replacement.
/// </summary>
public static class DotNetRefactoring
{

    // Project roots and the roots each project is permitted to reference. Used
    // to prune using directives that would point a layer at a namespace it is
    // not allowed to see (e.g. Application importing Infrastructure).
    private static readonly Dictionary<string, (string Own, string[] Referenced)> ProjectVisibility =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["src/Project.Domain"] = ("Project.Domain", []),
            ["src/Project.Application"] = ("Project.Application", ["Project.Domain"]),
            ["src/Project.Infrastructure"] = ("Project.Infrastructure", ["Project.Application", "Project.Domain"]),
            ["src/Project.API"] = ("Project.API", ["Project.Application", "Project.Infrastructure", "Project.Domain"])
        };

    /// <summary>
    /// Builds a mapping of old namespace -> new namespace from the plan's
    /// RenameNamespaceOrPackage operations. The worker computes this once and
    /// uses it to re-target every using directive in the migrated compilation.
    /// </summary>
    public static Dictionary<string, string> BuildNamespaceMap(MigrationPlan plan)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var op in plan.Operations.Where(o => o.Type == OperationType.RenameNamespaceOrPackage))
        {
            var oldNs = op.Parameters.GetValueOrDefault("OldNamespace", "");
            var newNs = op.Parameters.GetValueOrDefault("NewNamespace", "");
            if (string.IsNullOrWhiteSpace(newNs)) continue;

            // When the old namespace is empty the rename applies to a single
            // file's own declaration and does not participate in the map.
            if (string.IsNullOrWhiteSpace(oldNs)) continue;

            if (!map.ContainsKey(oldNs))
            {
                map[oldNs] = newNs;
            }
            else if (!string.Equals(map[oldNs], newNs, StringComparison.Ordinal))
            {
                // A single old namespace split across multiple layers: map to a
                // sentinel so consumers add all targets instead of guessing.
                if (!map[oldNs].Contains('\u0001'))
                {
                    map[oldNs] = map[oldNs] + "\u0001" + newNs;
                }
                else if (!map[oldNs].Split('\u0001').Contains(newNs))
                {
                    map[oldNs] = map[oldNs] + "\u0001" + newNs;
                }
            }
        }
        return map;
    }

    public static List<RepositoryInterfaceSpec> BuildRepositorySpecs(MigrationPlan plan)
    {
        var specs = new List<RepositoryInterfaceSpec>();
        foreach (var op in plan.Operations.Where(o => o.Type == OperationType.ExtractInterface))
        {
            var implClass = op.Parameters.GetValueOrDefault("ImplementationClass", "");
            var interfaceName = op.Parameters.GetValueOrDefault("InterfaceName", "");
            var ns = op.Parameters.GetValueOrDefault("Namespace", "");
            if (string.IsNullOrWhiteSpace(implClass) ||
                string.IsNullOrWhiteSpace(interfaceName) ||
                string.IsNullOrWhiteSpace(ns))
            {
                continue;
            }

            var usings = op.Parameters.GetValueOrDefault("Usings", "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            specs.Add(new RepositoryInterfaceSpec(
                implClass,
                op.Parameters.GetValueOrDefault("ImplementationPath", ""),
                interfaceName,
                ns,
                op.DestinationPath ?? "",
                usings));
        }
        return specs;
    }

    /// <summary>
    /// Generates the interface source by mirroring the real public instance
    /// method signatures of the concrete implementation.
    /// </summary>
    public static string GenerateInterfaceSource(RepositoryInterfaceSpec spec, string implementationSource)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(implementationSource).GetRoot();
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == spec.ImplementationClass);

        var signatures = new List<string>();
        if (classDecl != null)
        {
            foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var isPublic = method.Modifiers.Any(SyntaxKind.PublicKeyword);
                var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);

                if (!isPublic || isStatic) continue;

                var returnType = method.ReturnType.ToString();
                var name = method.Identifier.Text;
                var typeParams = method.TypeParameterList?.ToString() ?? string.Empty;
                var parameters = method.ParameterList.ToString();
                signatures.Add($"    {returnType} {name}{typeParams}{parameters};");
            }
        }

        if (signatures.Count == 0)
        {
            signatures.Add($"    // TODO(manual): no public instance members were detected on {spec.ImplementationClass}.");
        }

        var sb = new System.Text.StringBuilder();
        foreach (var u in spec.Usings.Distinct(StringComparer.Ordinal))
        {
            sb.AppendLine($"using {u};");
        }
        if (spec.Usings.Count > 0)
        {
            sb.AppendLine();
        }

        sb.AppendLine($"namespace {spec.InterfaceNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public interface {spec.InterfaceName}");
        sb.AppendLine("{");
        foreach (var sig in signatures)
        {
            sb.AppendLine(sig);
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Rewrites every C# file in the working directory:
    ///   1. using directives are re-targeted through the namespace map,
    ///   2. the concrete repository type is replaced with its extracted
    ///      interface in all consumers (dependency inversion),
    ///   3. the implementation is augmented with the interface base type,
    ///   4. duplicate using directives are collapsed.
    /// </summary>
    public static async Task<List<string>> RewriteReferencesAsync(
        string workingDirectory,
        MigrationPlan plan,
        CancellationToken cancellationToken)
    {
        var namespaceMap = BuildNamespaceMap(plan);
        var specs = BuildRepositorySpecs(plan);
        var changes = new List<string>();

        // A file whose *own* old namespace was split across projects (e.g. the
        // old namespace also housed the exception-handler that moved to API)
        // loses sight of siblings that moved elsewhere. Re-add the other split
        // targets as usings scoped to that file.
        var fileSplitUsings = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var op in plan.Operations.Where(o => o.Type == OperationType.RenameNamespaceOrPackage))
        {
            var oldNs = op.Parameters.GetValueOrDefault("OldNamespace", "");
            var newNs = op.Parameters.GetValueOrDefault("NewNamespace", "");
            if (string.IsNullOrWhiteSpace(op.DestinationPath) || string.IsNullOrWhiteSpace(oldNs)) continue;
            if (!namespaceMap.TryGetValue(oldNs, out var mapped) || !mapped.Contains('\u0001')) continue;

            var others = mapped.Split('\u0001').Where(ns => ns != newNs).ToList();
            if (others.Count == 0) continue;

            var fileKey = Normalize(Path.Combine(workingDirectory, op.DestinationPath));
            if (!fileSplitUsings.TryGetValue(fileKey, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                fileSplitUsings[fileKey] = set;
            }
            foreach (var ns in others) set.Add(ns);
        }

        var typeMap = new Dictionary<string, (string InterfaceName, string InterfaceNamespace)>(StringComparer.Ordinal);
        foreach (var spec in specs)
        {
            typeMap[spec.ImplementationClass] = (spec.InterfaceName, spec.InterfaceNamespace);
        }

        var implByPath = specs
            .Where(s => !string.IsNullOrWhiteSpace(s.ImplementationPath))
            .GroupBy(s => Normalize(Path.Combine(workingDirectory, s.ImplementationPath)))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var file in EnumerateCsFiles(workingDirectory))
        {
            var normalized = Normalize(file);
            cancellationToken.ThrowIfCancellationRequested();

            var original = await File.ReadAllTextAsync(file, cancellationToken);
            SyntaxNode root = CSharpSyntaxTree.ParseText(original, path: file).GetRoot();

            var isImplementationFile = implByPath.TryGetValue(normalized, out var fileSpecs);
            var rewriter = new ReferenceRewriter(namespaceMap, typeMap);
            root = rewriter.Visit(root)!;

            var extraUsings = new HashSet<string>(rewriter.AddedUsings, StringComparer.Ordinal);

            if (fileSplitUsings.TryGetValue(normalized, out var splitUsings))
            {
                foreach (var ns in splitUsings) extraUsings.Add(ns);
            }

            if (isImplementationFile && fileSpecs != null)
            {
                foreach (var spec in fileSpecs)
                {
                    root = AddInterfaceBaseType(root, spec.ImplementationClass, spec.InterfaceName);
                    extraUsings.Add(spec.InterfaceNamespace);
                }
            }

            root = AddMissingUsings(root, extraUsings);
            root = DeduplicateUsings(root);
            root = PruneDisallowedUsings(root, Path.GetRelativePath(workingDirectory, file));

            var updated = root.ToFullString();
            if (!string.Equals(original, updated, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(file, updated, cancellationToken);
                changes.Add(Path.GetRelativePath(workingDirectory, file));
            }
        }

        return changes;
    }

    /// <summary>
    /// Drops using directives that reference a target namespace which the
    /// file's project is not permitted to depend on. This keeps the dependency
    /// graph acyclic after the concrete repository type is inverted into an
    /// Application-layer interface.
    /// </summary>
    private static SyntaxNode PruneDisallowedUsings(SyntaxNode root, string relativePath)
    {
        if (root is not CompilationUnitSyntax compilationUnit) return root;

        var normalized = relativePath.Replace('\\', '/');
        var project = ProjectVisibility.Keys.FirstOrDefault(k =>
            normalized.StartsWith(k + "/", StringComparison.OrdinalIgnoreCase));
        if (project == null) return root;

        var (own, referenced) = ProjectVisibility[project];
        var allowedRoots = new List<string> { own };
        allowedRoots.AddRange(referenced);

        bool IsAllowed(string ns)
        {
            if (!ns.StartsWith("Project.", StringComparison.Ordinal)) return true;
            return allowedRoots.Any(r => string.Equals(ns, r, StringComparison.Ordinal) ||
                                         ns.StartsWith(r + ".", StringComparison.Ordinal));
        }

        var kept = compilationUnit.Usings
            .Where(u => u.Name == null || IsAllowed(u.Name.ToString()))
            .ToList();

        return compilationUnit.WithUsings(SyntaxFactory.List(kept));
    }

    private static SyntaxNode AddInterfaceBaseType(SyntaxNode root, string className, string interfaceName)
    {
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == className);
        if (classDecl == null) return root;

        var alreadyImplements = classDecl.BaseList?.Types
            .Any(t => t.Type.ToString().Split('.').Last() == interfaceName) == true;
        if (alreadyImplements) return root;

        var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceName));
        ClassDeclarationSyntax updated;
        if (classDecl.BaseList == null || classDecl.BaseList.Types.Count == 0)
        {
            updated = classDecl.WithBaseList(
                SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)));
        }
        else
        {
            updated = classDecl.AddBaseListTypes(baseType);
        }

        return root.ReplaceNode(classDecl, updated);
    }

    private static SyntaxNode AddMissingUsings(SyntaxNode root, HashSet<string> namespaces)
    {
        if (namespaces.Count == 0) return root;

        var compilationUnit = root as CompilationUnitSyntax;
        if (compilationUnit == null) return root;

        var existing = compilationUnit.Usings
            .Where(u => u.Name != null)
            .Select(u => u.Name!.ToString())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var ns in namespaces)
        {
            if (existing.Contains(ns)) continue;

            // Parse a complete directive so the tokens carry the whitespace and
            // trailing newline that hand-built syntax nodes lack.
            var directive = SyntaxFactory.ParseCompilationUnit($"using {ns};\n").Usings[0];
            compilationUnit = compilationUnit.AddUsings(directive);
            existing.Add(ns);
        }

        return compilationUnit;
    }

    private static SyntaxNode DeduplicateUsings(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit) return root;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var usings = new List<UsingDirectiveSyntax>();
        foreach (var u in compilationUnit.Usings)
        {
            var name = u.Name?.ToString() ?? "";
            if (!seen.Add(name)) continue;
            usings.Add(u);
        }

        return compilationUnit.WithUsings(SyntaxFactory.List(usings));
    }

    /// <summary>
    /// Removes the superseded legacy project (and now-empty legacy folders)
    /// after the new clean architecture projects have been produced.
    /// </summary>
    public static List<string> RemoveLegacyArtifacts(string workingDirectory, string relativePath)
    {
        var removed = new List<string>();
        var full = Path.GetFullPath(Path.Combine(workingDirectory, relativePath));
        if (!full.StartsWith(Path.GetFullPath(workingDirectory), StringComparison.OrdinalIgnoreCase))
        {
            return removed;
        }

        if (File.Exists(full))
        {
            File.Delete(full);
            removed.Add(relativePath);
        }
        else if (Directory.Exists(full))
        {
            Directory.Delete(full, true);
            removed.Add(relativePath);
        }

        // Prune empty legacy directories but never touch the target src/ tree.
        foreach (var dir in Directory.GetDirectories(workingDirectory, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length))
        {
            if (dir.Contains(Path.Combine(workingDirectory, "src"), StringComparison.OrdinalIgnoreCase)) continue;
            if (dir.Contains("/obj/") || dir.Contains("/bin/")) continue;
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir, true);
            }
        }

        return removed;
    }

    /// <summary>
    /// Generates a real Infrastructure composition extension that registers the
    /// DbContext and every repository interface -> implementation binding.
    /// </summary>
    public static string GenerateInfrastructureDependencyInjection(
        string workingDirectory,
        string infrastructureNamespace,
        IReadOnlyList<RepositoryInterfaceSpec> specs)
    {
        var dbContext = FindDbContext(workingDirectory);
        var repositoryNamespaces = specs
            .Select(s => s.InterfaceNamespace)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var implementationNamespaces = specs
            .Select(s => ResolveNamespace(workingDirectory, s.ImplementationPath, infrastructureNamespace))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        foreach (var ns in repositoryNamespaces.Concat(implementationNamespaces).Distinct(StringComparer.Ordinal))
        {
            sb.AppendLine($"using {ns};");
        }
        sb.AppendLine($"using {infrastructureNamespace};");
        sb.AppendLine();
        sb.AppendLine($"namespace {infrastructureNamespace};");
        sb.AppendLine();
        sb.AppendLine("public static class DependencyInjection");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection AddInfrastructure(this IServiceCollection services)");
        sb.AppendLine("    {");
        if (dbContext != null)
        {
            sb.AppendLine($"        services.AddDbContext<{dbContext.Value.Name}>(options => options.UseSqlite(\"Data Source=app.db\"));");
        }
        foreach (var spec in specs)
        {
            var implNamespace = ResolveNamespace(workingDirectory, spec.ImplementationPath, infrastructureNamespace);
            sb.AppendLine($"        services.AddScoped<{spec.InterfaceName}, {implNamespace}.{spec.ImplementationClass}>();");
        }
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates the API composition root (Program.cs) that wires Application
    /// services and the Infrastructure registrations together.
    /// </summary>
    public static string GenerateApiCompositionRoot(
        string workingDirectory,
        IReadOnlyList<string> applicationServiceNamespaces,
        IReadOnlyList<string> applicationServiceTypes,
        string infrastructureNamespace)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ns in applicationServiceNamespaces.Distinct(StringComparer.Ordinal))
        {
            sb.AppendLine($"using {ns};");
        }
        sb.AppendLine($"using {infrastructureNamespace};");
        sb.AppendLine();
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine();
        sb.AppendLine("builder.Services.AddControllers();");
        sb.AppendLine("builder.Services.AddInfrastructure();");
        foreach (var svc in applicationServiceTypes.Distinct(StringComparer.Ordinal))
        {
            sb.AppendLine($"builder.Services.AddScoped<{svc}>();");
        }
        sb.AppendLine();
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine();
        sb.AppendLine("app.MapControllers();");
        sb.AppendLine();
        sb.AppendLine("app.Run();");
        return sb.ToString();
    }

    public static (string Name, string Namespace)? FindDbContext(string workingDirectory)
    {
        foreach (var file in EnumerateCsFiles(workingDirectory))
        {
            try
            {
                var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
                var fileNs = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString() ?? "";

                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var baseName = classDecl.BaseList?.Types
                        .Select(t => t.Type.ToString().Split('.').Last())
                        .FirstOrDefault();
                    if (string.Equals(baseName, "DbContext", StringComparison.Ordinal))
                    {
                        return (classDecl.Identifier.Text, fileNs);
                    }
                }
            }
            catch
            {
                // Ignore unparseable files during discovery.
            }
        }
        return null;
    }

    public static List<string> FindApplicationServiceTypes(string workingDirectory)
    {
        var result = new List<string>();
        var servicesDir = Path.Combine(workingDirectory, "src", "Project.Application", "Services");
        if (!Directory.Exists(servicesDir)) return result;

        foreach (var file in Directory.GetFiles(servicesDir, "*.cs"))
        {
            var text = File.ReadAllText(file);
            var match = System.Text.RegularExpressions.Regex.Match(
                text, @"namespace\s+([A-Za-z0-9_\.]+)\s*;[\s\S]*?public\s+(?:sealed\s+)?class\s+([A-Za-z0-9_]+)");
            if (match.Success)
            {
                result.Add($"{match.Groups[1].Value}.{match.Groups[2].Value}");
            }
        }
        return result;
    }

    public static List<string> FindApplicationServiceNamespaces(string workingDirectory)
        => FindApplicationServiceTypes(workingDirectory)
            .Select(t => t.Substring(0, t.LastIndexOf('.')))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static string ResolveNamespace(string workingDirectory, string relativePath, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            var full = Path.Combine(workingDirectory, relativePath);
            if (File.Exists(full))
            {
                var root = CSharpSyntaxTree.ParseText(File.ReadAllText(full)).GetRoot();
                var ns = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
                if (ns != null) return ns.Name.ToString();
                var blockNs = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if (blockNs != null) return blockNs.Name.ToString();
            }
        }
        return fallback;
    }

    public static IEnumerable<string> EnumerateCsFiles(string workingDirectory)
    {
        return Directory.EnumerateFiles(workingDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsNoiseFile(f));
    }

    private static bool IsNoiseFile(string path)
    {
        var p = path.Replace('\\', '/');
        if (p.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/__MACOSX/", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("/.git/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = Path.GetFileName(p);
        return fileName.StartsWith("._", StringComparison.Ordinal) ||
               string.Equals(fileName, ".DS_Store", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) => Path.GetFullPath(path).Replace('\\', '/');

    private sealed class ReferenceRewriter : CSharpSyntaxRewriter
    {
        private readonly IReadOnlyDictionary<string, string> _namespaceMap;
        private readonly IReadOnlyDictionary<string, (string InterfaceName, string InterfaceNamespace)> _typeMap;
        public HashSet<string> AddedUsings { get; } = new(StringComparer.Ordinal);

        public ReferenceRewriter(
            IReadOnlyDictionary<string, string> namespaceMap,
            IReadOnlyDictionary<string, (string InterfaceName, string InterfaceNamespace)> typeMap)
        {
            _namespaceMap = namespaceMap;
            _typeMap = typeMap;
        }

        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name != null && _namespaceMap.TryGetValue(node.Name.ToString(), out var mapped))
            {
                var expanded = mapped.Split('\u0001');
                if (expanded.Length == 1)
                {
                    AddedUsings.Add(expanded[0]);
                    return node.WithName(SyntaxFactory.ParseName(expanded[0]).WithTriviaFrom(node.Name));
                }

                // One old namespace split across layers: add all targets and
                // neutralise the original directive.
                foreach (var ns in expanded) AddedUsings.Add(ns);
                return SyntaxFactory.ParseCompilationUnit($"using {expanded[0]};\n").Usings[0]
                    .WithTriviaFrom(node);
            }
            return base.VisitUsingDirective(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_typeMap.TryGetValue(node.Identifier.Text, out var mapping))
            {
                AddedUsings.Add(mapping.InterfaceNamespace);
                return SyntaxFactory.IdentifierName(mapping.InterfaceName).WithTriviaFrom(node);
            }
            return base.VisitIdentifierName(node);
        }
    }
}

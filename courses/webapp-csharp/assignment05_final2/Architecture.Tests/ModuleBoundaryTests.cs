using System.Xml.Linq;

namespace Architecture.Tests;

/// <summary>
/// Safety net for the Final2 modular monolith. Tests enforce the boundaries
/// documented in <c>docs/final2-module-map.md</c> and Risk-fix prompt 3.
///
/// Phase 1 created this project as a placeholder. Phase 2 created the
/// <c>Modules.*</c>, <c>SharedKernel</c>, and <c>Shared.Contracts</c> shells,
/// at which point the rules below become active.
///
/// Per Phase 1 instructions, legacy <c>App.*</c> references from
/// <c>Modules.*</c> are still allowed — that rule will be tightened in
/// Phase 10. See <see cref="ModulesMustNotReferenceLegacyAppProjects_NotEnforcedYet"/>.
/// </summary>
[Trait("Category", "Architecture")]
public class ModuleBoundaryTests
{
    private const string ModulesPrefix = "Modules.";

    private static readonly string[] ExpectedModules =
    {
        "Modules.Users",
        "Modules.Gyms",
        "Modules.Memberships",
        "Modules.Training",
        "Modules.Maintenance",
    };

    private static readonly Dictionary<string, string> ModuleDbContextTypes = new(StringComparer.Ordinal)
    {
        ["Modules.Users"] = "UsersDbContext",
        ["Modules.Gyms"] = "GymsDbContext",
        ["Modules.Memberships"] = "MembershipsDbContext",
        ["Modules.Training"] = "TrainingDbContext",
        ["Modules.Maintenance"] = "MaintenanceDbContext",
    };

    private static readonly string[] AllowedNonModuleProjectRefsFromModules =
    {
        "SharedKernel",
        "Shared.Contracts",
        "App.Resources",

        // Phase 4-10 transitional: service contracts, EF bridge access, and
        // domain entities still move module-by-module. The concrete App.BLL
        // dependency was removed in Phase 10e. App.DAL.Contracts was removed
        // from module references during Phase 10f.
        // App.DTO removed in Phase 10b — all DTOs now live in Shared.Contracts.
        "App.BLL.Contracts",
        "App.DAL.EF",
        "App.Domain",
    };

    private static readonly string[] LegacyAppProjectsBlockedAfterPhase10 =
    {
        "App.BLL",
        "App.BLL.Contracts",
        "App.DAL.Contracts",
        "App.DAL.EF",
        "App.Domain",
        "App.DTO",
    };

    [Fact]
    public void ExpectedModuleProjects_AllExist()
    {
        var root = ResolveAssignmentRoot();
        foreach (var module in ExpectedModules)
        {
            var csproj = Path.Combine(root, module, $"{module}.csproj");
            Assert.True(File.Exists(csproj), $"Missing module project: {csproj}");
        }
    }

    [Fact]
    public void Modules_DoNotReferenceOtherModules()
    {
        var root = ResolveAssignmentRoot();
        var moduleProjects = DiscoverModuleProjects(root);
        Assert.NotEmpty(moduleProjects);

        foreach (var project in moduleProjects)
        {
            var ownName = Path.GetFileNameWithoutExtension(project);
            var foreignModuleRefs = GetProjectReferences(project)
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .Where(name => name.StartsWith(ModulesPrefix, StringComparison.Ordinal))
                .Where(name => !string.Equals(name, ownName, StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                foreignModuleRefs.Length == 0,
                $"{ownName} must not reference other modules but references: {string.Join(", ", foreignModuleRefs)}");
        }
    }

    [Fact]
    public void Modules_OnlyReferenceAllowedSharedProjects()
    {
        var root = ResolveAssignmentRoot();
        var moduleProjects = DiscoverModuleProjects(root);
        Assert.NotEmpty(moduleProjects);

        foreach (var project in moduleProjects)
        {
            var ownName = Path.GetFileNameWithoutExtension(project);
            var disallowed = GetProjectReferences(project)
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .Where(name => !name.StartsWith(ModulesPrefix, StringComparison.Ordinal))
                .Where(name => !AllowedNonModuleProjectRefsFromModules.Contains(name, StringComparer.Ordinal))
                .ToArray();

            Assert.True(
                disallowed.Length == 0,
                $"{ownName} may only reference {string.Join(", ", AllowedNonModuleProjectRefsFromModules)} " +
                $"outside of Modules.* but also references: {string.Join(", ", disallowed)}");
        }
    }

    [Fact]
    public void WebApp_ReferencesAllExpectedModules()
    {
        var root = ResolveAssignmentRoot();
        var webApp = Path.Combine(root, "WebApp", "WebApp.csproj");
        Assert.True(File.Exists(webApp), $"Expected WebApp.csproj at {webApp}");

        var refs = GetProjectReferences(webApp)
            .Select(reference => Path.GetFileNameWithoutExtension(reference))
            .ToHashSet(StringComparer.Ordinal);

        var missing = ExpectedModules.Where(m => !refs.Contains(m)).ToArray();
        Assert.True(
            missing.Length == 0,
            $"WebApp must reference all modules but is missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void WebAppAndModules_DoNotReferenceLegacyAppBllImplementationProject()
    {
        var root = ResolveAssignmentRoot();
        var projects = DiscoverModuleProjects(root)
            .Concat(new[] { Path.Combine(root, "WebApp", "WebApp.csproj") })
            .ToArray();

        foreach (var project in projects)
        {
            var ownName = Path.GetFileNameWithoutExtension(project);
            var legacyBllRefs = GetProjectReferences(project)
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .Where(name => string.Equals(name, "App.BLL", StringComparison.Ordinal))
                .ToArray();

            Assert.True(
                legacyBllRefs.Length == 0,
                $"{ownName} must not reference the legacy App.BLL implementation project.");
        }
    }

    [Fact]
    public void ModuleDbContexts_LiveInOwningModulePersistenceFolders()
    {
        var root = ResolveAssignmentRoot();

        foreach (var (module, contextType) in ModuleDbContextTypes)
        {
            var expectedPath = Path.Combine(
                root,
                module,
                "Infrastructure",
                "Persistence",
                $"{contextType}.cs");

            Assert.True(File.Exists(expectedPath), $"{contextType} must live in {module}/Infrastructure/Persistence.");
        }
    }

    [Fact]
    public void SharedModulePersistenceInfrastructure_LivesInSharedKernel()
    {
        var root = ResolveAssignmentRoot();

        var expectedSharedFiles = new[]
        {
            Path.Combine(root, "SharedKernel", "Persistence", "IGymContext.cs"),
            Path.Combine(root, "SharedKernel", "Persistence", "ModuleDbContextBase.cs"),
            Path.Combine(root, "SharedKernel", "Persistence", "ModuleDbContextRegistrationExtensions.cs"),
        };

        foreach (var expectedFile in expectedSharedFiles)
        {
            Assert.True(File.Exists(expectedFile), $"Expected shared persistence file at {expectedFile}");
        }

        var legacyPersistenceFiles = new[]
        {
            Path.Combine(root, "App.DAL.EF", "Tenant", "IGymContext.cs"),
            Path.Combine(root, "App.DAL.EF", "ModularPersistence", "ModuleDbContextBase.cs"),
            Path.Combine(root, "App.DAL.EF", "ModularPersistence", "ModuleDbContextRegistrationExtensions.cs"),
        };

        foreach (var legacyFile in legacyPersistenceFiles)
        {
            Assert.False(File.Exists(legacyFile), $"Shared module persistence infrastructure must not remain in App.DAL.EF: {legacyFile}");
        }
    }

    [Fact]
    public void Modules_DoNotReferenceForeignModuleDbContexts()
    {
        var root = ResolveAssignmentRoot();

        foreach (var module in ExpectedModules)
        {
            var moduleRoot = Path.Combine(root, module);
            var ownContext = ModuleDbContextTypes[module];
            var foreignContexts = ModuleDbContextTypes
                .Where(pair => !string.Equals(pair.Key, module, StringComparison.Ordinal))
                .Select(pair => pair.Value)
                .ToArray();

            var offenders = Directory
                .EnumerateFiles(moduleRoot, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                               !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(file => new
                {
                    File = Path.GetRelativePath(root, file),
                    ForeignContexts = foreignContexts
                        .Where(context => File.ReadAllText(file).Contains(context, StringComparison.Ordinal))
                        .ToArray(),
                })
                .Where(result => result.ForeignContexts.Length > 0)
                .ToArray();

            Assert.True(
                offenders.Length == 0,
                $"{module} may only use {ownContext}; foreign DbContext references found: " +
                string.Join("; ", offenders.Select(result => $"{result.File} => {string.Join(", ", result.ForeignContexts)}")));
        }
    }

    [Fact(Skip = "Enforced from Phase 10 onward — Phase 1 explicitly permits legacy App.* references because code has not moved yet.")]
    public void ModulesMustNotReferenceLegacyAppProjects_NotEnforcedYet()
    {
        var root = ResolveAssignmentRoot();
        var moduleProjects = DiscoverModuleProjects(root);

        foreach (var project in moduleProjects)
        {
            var ownName = Path.GetFileNameWithoutExtension(project);
            var legacyRefs = GetProjectReferences(project)
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .Where(name => LegacyAppProjectsBlockedAfterPhase10.Contains(name, StringComparer.Ordinal))
                .ToArray();

            Assert.True(
                legacyRefs.Length == 0,
                $"{ownName} must not reference legacy App.* projects but references: {string.Join(", ", legacyRefs)}");
        }
    }

    private static IReadOnlyList<string> DiscoverModuleProjects(string root)
    {
        return Directory
            .EnumerateDirectories(root, "Modules.*", SearchOption.TopDirectoryOnly)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
            .ToArray();
    }

    private static IReadOnlyList<string> GetProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        return doc
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static string ResolveAssignmentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "multi-gym-management-system.slnx");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate assignment root (multi-gym-management-system.slnx).");
    }
}

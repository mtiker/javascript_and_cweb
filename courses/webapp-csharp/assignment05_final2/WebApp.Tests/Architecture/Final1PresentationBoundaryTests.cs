namespace WebApp.Tests.Architecture;

public class Final1PresentationBoundaryTests
{
    private static readonly System.Text.RegularExpressions.Regex ConcreteAppDbContextReference =
        new(@"\bAppDbContext\b", System.Text.RegularExpressions.RegexOptions.Compiled);

    [Fact]
    public void MvcControllersAndViewComponents_DoNotInjectAppDbContext()
    {
        var root = ResolveAssignmentRoot();
        var files = Directory
            .GetFiles(Path.Combine(root, "WebApp"), "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                path.Contains($"{Path.DirectorySeparatorChar}Areas{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                path.Contains($"{Path.DirectorySeparatorChar}Controllers{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                path.Contains($"{Path.DirectorySeparatorChar}Controllers{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                path.Contains($"{Path.DirectorySeparatorChar}ViewComponents{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.False(ConcreteAppDbContextReference.IsMatch(source), $"{file} directly references AppDbContext.");
            Assert.DoesNotContain("using App.DAL.EF;", source);
        }
    }

    [Fact]
    public void AdminAndClientPageServices_DoNotInjectConcreteAppDbContext()
    {
        var root = ResolveAssignmentRoot();
        var serviceRoots = new[]
        {
            Path.Combine(root, "WebApp", "Areas", "Admin", "Services"),
            Path.Combine(root, "WebApp", "Areas", "Client", "Services")
        };

        var files = serviceRoots.SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)).ToArray();

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.False(ConcreteAppDbContextReference.IsMatch(source), $"{file} directly references AppDbContext.");
            Assert.DoesNotContain("using App.DAL.EF;", source);
        }
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

        throw new InvalidOperationException("Could not locate assignment root.");
    }
}

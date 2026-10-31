using Corely.IAM.Services;

namespace Corely.IAM.Web.UnitTests.Components;

public class KeyRevealSurfaceTests
{
    private static readonly string[] _apiFolders = ["Corely.IAM.Web", "Corely.IAM.WebApp"];

    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "Corely.IAM.slnx")))
            folder = folder.Parent;

        Assert.NotNull(folder);
        return folder.FullName;
    }

    [Fact]
    public void NoEndpointCode_CallsTheKeyReveal()
    {
        var root = RepositoryRoot();

        var callers = _apiFolders
            .SelectMany(project =>
                Directory.EnumerateFiles(
                    Path.Combine(root, project),
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            .Where(file =>
                !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
            )
            .Where(file =>
                File.ReadAllText(file).Contains(nameof(IRetrievalService.RevealAccountKeyAsync))
            );

        Assert.Empty(callers);
    }

    [Fact]
    public void OnlyTheAccountPage_OffersTheKeyReveal()
    {
        var components = Path.Combine(RepositoryRoot(), "Corely.IAM.Web", "Components");

        var callers = Directory
            .EnumerateFiles(components, "*.razor", SearchOption.AllDirectories)
            .Where(file =>
                File.ReadAllText(file).Contains(nameof(IRetrievalService.RevealAccountKeyAsync))
            )
            .Select(Path.GetFileName);

        Assert.Equal(["AccountDetail.razor"], callers);
    }
}

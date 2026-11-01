namespace Corely.IAM.Web.UnitTests.Components;

public class YourAccessCoverageTests
{
    private static string ComponentsFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(folder!.FullName, "Corely.IAM.slnx")))
            folder = folder.Parent;
        return Path.Combine(folder.FullName, "Corely.IAM.Web", "Components");
    }

    [Fact]
    public void EveryListAndDetailPage_ShowsYourAccess()
    {
        var missing = Directory
            .EnumerateFiles(ComponentsFolder(), "*.razor", SearchOption.AllDirectories)
            .Select(file => (Name: Path.GetFileName(file), Text: File.ReadAllText(file)))
            .Where(page =>
                page.Text.Contains("@inherits EntityListPageBase")
                || page.Text.Contains("@inherits EntityDetailPageBase")
            )
            .Where(page =>
                !page.Text.Contains("<ResourceAccessPanel ")
                && !page.Text.Contains("<EffectivePermissionsPanel ")
            )
            .Select(page => page.Name);

        Assert.Empty(missing);
    }
}

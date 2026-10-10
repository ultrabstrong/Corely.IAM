using Corely.IAM.Audits.Models;

namespace Corely.IAM.UnitTests.Audits.Models;

public class AuditOptionsTests
{
    [Fact]
    public void SourceName_ReturnsTheConfiguredSource()
    {
        Assert.Equal("portal", new AuditOptions { Source = "portal" }.SourceName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SourceName_FallsBackToTheEntryAssembly_WhenNoSourceIsConfigured(string? source)
    {
        var name = new AuditOptions { Source = source }.SourceName();

        Assert.False(string.IsNullOrWhiteSpace(name));
    }
}

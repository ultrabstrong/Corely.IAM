using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class PathStringExtensionsTests
{
    [Theory]
    [InlineData("/_framework/blazor.web.js")]
    [InlineData("/_FRAMEWORK/anything")]
    [InlineData("/_content/Corely.IAM.Web/app.css")]
    [InlineData("/_content/Corely.IAM.Web/lib/no-extension")]
    [InlineData("/css/site.css")]
    [InlineData("/img/logo.png")]
    [InlineData("/fonts/x.woff2")]
    [InlineData("/site.webmanifest")]
    public void IsCacheableStaticAsset_IsTrue_ForFrameworkContentAndStaticFiles(string path)
    {
        Assert.True(new PathString(path).IsCacheableStaticAsset());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/signin")]
    [InlineData("/api/data.json")]
    [InlineData("/report.pdf")]
    [InlineData("/framework/x.js.bak")]
    [InlineData("/img/logo.PNG")]
    public void IsCacheableStaticAsset_IsFalse_ForPagesAndOtherFiles(string path)
    {
        Assert.False(new PathString(path).IsCacheableStaticAsset());
    }

    [Fact]
    public void IsCacheableStaticAsset_IsFalse_ForAnEmptyPath()
    {
        Assert.False(PathString.Empty.IsCacheableStaticAsset());
    }
}

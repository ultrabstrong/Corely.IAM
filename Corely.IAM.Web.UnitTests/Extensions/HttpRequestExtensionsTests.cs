using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class HttpRequestExtensionsTests
{
    private static HttpRequest Request(string? fetchSite)
    {
        var context = new DefaultHttpContext();
        if (fetchSite is not null)
            context.Request.Headers["Sec-Fetch-Site"] = fetchSite;
        return context.Request;
    }

    [Fact]
    public void IsSameOrigin_IsTrue_ForSameOrigin() =>
        Assert.True(Request("same-origin").IsSameOrigin());

    [Theory]
    [InlineData("cross-site")]
    [InlineData("same-site")]
    [InlineData("none")]
    [InlineData(null)]
    public void IsSameOrigin_IsFalse_ForAnyOtherOriginOrNoHeader(string? fetchSite) =>
        Assert.False(Request(fetchSite).IsSameOrigin());
}

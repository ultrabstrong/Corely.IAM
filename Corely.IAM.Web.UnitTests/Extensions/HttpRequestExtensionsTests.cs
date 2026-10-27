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

    private static HttpRequest WithAuthorization(string? authorization)
    {
        var context = new DefaultHttpContext();
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;
        return context.Request;
    }

    [Theory]
    [InlineData("Bearer abc.def", "abc.def")]
    [InlineData("bearer   abc.def  ", "abc.def")]
    [InlineData("Bearer ", null)]
    [InlineData("Basic dXNlcg==", null)]
    [InlineData(null, null)]
    public void BearerToken_ReadsTheTokenAfterTheScheme(string? authorization, string? expected) =>
        Assert.Equal(expected, WithAuthorization(authorization).BearerToken());

    [Theory]
    [InlineData("Bearer abc", true)]
    [InlineData("Bearer ", true)]
    [InlineData("Basic dXNlcg==", false)]
    [InlineData(null, false)]
    public void HasBearerToken_IsTrue_ForTheBearerScheme(string? authorization, bool expected) =>
        Assert.Equal(expected, WithAuthorization(authorization).HasBearerToken());
}

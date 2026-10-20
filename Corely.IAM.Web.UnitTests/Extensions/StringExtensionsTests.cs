using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/accounts/1/users")]
    [InlineData("/grants?status=open")]
    public void AsLocalUrl_KeepsTheUrl_ForAPathOnThisSite(string url)
    {
        Assert.Equal(url, url.AsLocalUrl());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("evil.example")]
    [InlineData("~/home")]
    [InlineData("/home\r\nLocation: https://evil.example")]
    public void AsLocalUrl_IsNull_ForAnythingThatCouldLeaveTheSite(string? url)
    {
        Assert.Null(url.AsLocalUrl());
    }
}

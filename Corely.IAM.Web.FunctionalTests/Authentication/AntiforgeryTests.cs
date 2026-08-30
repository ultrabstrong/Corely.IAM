using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F11 - the antiforgery guard actually fires. Every other test in this suite posts *with* a
/// token, so without these the guard could be disabled entirely and nothing would notice.
/// </summary>
public class AntiforgeryTests : FunctionalTestBase
{
    [Fact]
    public async Task SignInPost_WithoutAntiforgeryToken_IsRejected()
    {
        using var response = await Client.PostFormWithoutAntiforgeryAsync(
            AppRoutes.SignIn,
            new Dictionary<string, string>
            {
                ["Username"] = SeedData.OwnerUsername,
                ["Password"] = SeedData.OwnerPassword,
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task SignInPost_WithGarbageAntiforgeryToken_IsRejected()
    {
        using var response = await Client.PostFormWithoutAntiforgeryAsync(
            AppRoutes.SignIn,
            new Dictionary<string, string>
            {
                ["Username"] = SeedData.OwnerUsername,
                ["Password"] = SeedData.OwnerPassword,
                ["__RequestVerificationToken"] = "not-a-real-token",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task RejectedPost_DoesNotLeakAStackTrace()
    {
        using var response = await Client.PostFormWithoutAntiforgeryAsync(
            AppRoutes.SignIn,
            new Dictionary<string, string> { ["Username"] = "x", ["Password"] = "y" }
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("AntiforgeryValidationException", body);
        Assert.DoesNotContain("at Microsoft.AspNetCore", body);
    }
}

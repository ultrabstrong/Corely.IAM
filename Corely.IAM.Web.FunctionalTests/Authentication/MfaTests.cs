using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F7 - the MFA challenge. The critical assertion is the negative one: a correct password on an
/// MFA-enabled account must not, on its own, produce a usable session.
/// </summary>
public class MfaTests : FunctionalTestBase
{
    [Fact]
    public async Task SignIn_WithMfaEnabled_RedirectsToChallenge()
    {
        await EnableMfaAsync();

        using var response = await SignInAsync();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.VerifyMfa, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task SignIn_WithMfaEnabled_IssuesNoAuthCookiesBeforeVerification()
    {
        await EnableMfaAsync();

        using var response = await SignInAsync();
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task PreMfaState_DoesNotGrantAccessToProtectedPages()
    {
        await EnableMfaAsync();
        using (var signIn = await SignInAsync())
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);

        using var response = await Client.GetAsync(AppRoutes.Dashboard);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task VerifyMfa_WithCorrectCode_CompletesSignIn()
    {
        var secret = await EnableMfaAsync();
        var challenge = await StartMfaChallengeAsync();
        var code = await GenerateTotpCodeAsync(secret);

        using var response = await Client.PostFormAsync(
            AppRoutes.VerifyMfa,
            new Dictionary<string, string>
            {
                ["Code"] = code,
                ["MfaChallengeToken"] = challenge.ChallengeToken,
            },
            challenge.AntiforgeryToken
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(HasAuthCookies);
    }

    [Fact]
    public async Task VerifyMfa_WithIncorrectCode_IsRejected()
    {
        await EnableMfaAsync();
        var challenge = await StartMfaChallengeAsync();

        using var response = await Client.PostFormAsync(
            AppRoutes.VerifyMfa,
            new Dictionary<string, string>
            {
                ["Code"] = "000000",
                ["MfaChallengeToken"] = challenge.ChallengeToken,
            },
            challenge.AntiforgeryToken
        );

        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task VerifyMfa_WithoutAChallengeToken_RedirectsToSignIn()
    {
        await EnableMfaAsync();

        using var response = await Client.GetAsync(AppRoutes.VerifyMfa);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task VerifyMfa_AfterChallengeTimeout_DoesNotSignIn()
    {
        var secret = await EnableMfaAsync();
        var challenge = await StartMfaChallengeAsync();

        // MfaChallengeTimeoutSeconds is 300 in the test host configuration.
        Clock.AdvanceSeconds(400);
        var code = await GenerateTotpCodeAsync(secret);

        using var response = await Client.PostFormAsync(
            AppRoutes.VerifyMfa,
            new Dictionary<string, string>
            {
                ["Code"] = code,
                ["MfaChallengeToken"] = challenge.ChallengeToken,
            },
            challenge.AntiforgeryToken
        );

        Assert.False(HasAuthCookies);
    }

    /// <summary>
    /// Signs in to obtain a challenge, then reads the challenge token and the antiforgery token
    /// off the rendered form in a single GET - the same way a browser would carry them forward.
    ///
    /// The page must only be fetched once: it reads its challenge token from TempData, so a
    /// second GET would find nothing and redirect to sign-in.
    /// </summary>
    private async Task<MfaChallenge> StartMfaChallengeAsync()
    {
        using (var signIn = await SignInAsync())
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);

        using var challengePage = await Client.GetAsync(AppRoutes.VerifyMfa);
        Assert.Equal(HttpStatusCode.OK, challengePage.StatusCode);

        var html = await challengePage.Content.ReadAsStringAsync();
        var challengeToken = Html.InputValue(html, "MfaChallengeToken");
        var antiforgeryToken = Html.InputValue(html, "__RequestVerificationToken");

        Assert.False(
            string.IsNullOrWhiteSpace(challengeToken),
            "Challenge token missing from the form."
        );
        return new MfaChallenge(challengeToken!, antiforgeryToken);
    }

    private sealed record MfaChallenge(string ChallengeToken, string? AntiforgeryToken);
}

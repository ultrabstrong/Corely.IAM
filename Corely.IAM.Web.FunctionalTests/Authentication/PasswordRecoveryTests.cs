using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.WebApp;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

/// <summary>
/// F10 - password recovery end to end through the reference host's pages.
///
/// Assertions check page content rather than status codes: this host re-renders the same page
/// with a 200 whether the reset succeeded or failed, so asserting on status alone lets a broken
/// reset pass silently. It did exactly that until a missing ConfirmPassword field was found.
///
/// Note on user enumeration: this host deliberately surfaces library result codes directly, so an
/// unknown email renders a distinguishable error. That is documented intent for a demo host, not
/// a defect, so it is asserted as-is rather than as a no-enumeration guarantee.
/// </summary>
public class PasswordRecoveryTests : FunctionalTestBase
{
    private const string NewPassword = "Rotated!Pass456";
    private const string ThirdPassword = "Another!Pass789";
    private const string ResetSucceededMarker = "Password reset complete";

    [Fact]
    public async Task RequestRecovery_ForKnownUser_IssuesAToken()
    {
        var token = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task RequestRecovery_ForUnknownUser_DoesNotReachThePreviewPage()
    {
        using var response = await Client.PostFormAsync(
            WebAppRoutes.ForgotPassword,
            new Dictionary<string, string> { ["Email"] = "nobody@example.com" }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RedeemingAValidToken_ChangesThePassword()
    {
        var token = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);

        await AssertResetSucceedsAsync(token!, NewPassword);

        using var signIn = await SignInAsync(password: NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.True(HasAuthCookies);
    }

    [Fact]
    public async Task AfterReset_TheOldPasswordNoLongerWorks()
    {
        var token = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);
        await AssertResetSucceedsAsync(token!, NewPassword);

        using var signIn = await SignInAsync(password: SeedData.OwnerPassword);

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task AToken_CannotBeRedeemedTwice()
    {
        var token = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);
        await AssertResetSucceedsAsync(token!, NewPassword);

        await AssertResetFailsAsync(token!, ThirdPassword);

        using var signIn = await SignInAsync(password: ThirdPassword);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task AGarbageToken_IsRejected()
    {
        await AssertResetFailsAsync("not-a-real-token", NewPassword);

        using var signIn = await SignInAsync(password: NewPassword);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task MismatchedConfirmation_IsRejected()
    {
        var token = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);

        using var reset = await Client.PostFormAsync(
            WebAppRoutes.ResetPassword,
            new Dictionary<string, string>
            {
                ["Token"] = token!,
                ["Password"] = NewPassword,
                ["ConfirmPassword"] = ThirdPassword,
            }
        );
        Assert.DoesNotContain(
            ResetSucceededMarker,
            await reset.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );

        using var signIn = await SignInAsync(password: NewPassword);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task RequestingASecondRecovery_InvalidatesTheFirstToken()
    {
        var first = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);
        var second = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);
        Assert.NotEqual(first, second);

        await AssertResetFailsAsync(first!, NewPassword);

        using var signIn = await SignInAsync(password: NewPassword);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task TheMostRecentRecoveryTokenStillWorks()
    {
        await RequestRecoveryTokenAsync(SeedData.OwnerEmail);
        var second = await RequestRecoveryTokenAsync(SeedData.OwnerEmail);

        await AssertResetSucceedsAsync(second!, NewPassword);

        using var signIn = await SignInAsync(password: NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
    }

    private Task<HttpResponseMessage> ResetAsync(string token, string password) =>
        Client.PostFormAsync(
            WebAppRoutes.ResetPassword,
            new Dictionary<string, string>
            {
                ["Token"] = token,
                ["Password"] = password,
                ["ConfirmPassword"] = password,
            }
        );

    private async Task AssertResetSucceedsAsync(string token, string password)
    {
        using var response = await ResetAsync(token, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            ResetSucceededMarker,
            await response.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private async Task AssertResetFailsAsync(string token, string password)
    {
        using var response = await ResetAsync(token, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            ResetSucceededMarker,
            await response.Content.ReadAsStringAsync(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Drives the real pages: post the email, follow the demo preview redirect, and read the token
    /// off the rendered page - the reference host's stand-in for email delivery.
    /// </summary>
    private async Task<string?> RequestRecoveryTokenAsync(string email)
    {
        using var request = await Client.PostFormAsync(
            WebAppRoutes.ForgotPassword,
            new Dictionary<string, string> { ["Email"] = email }
        );
        Assert.Equal(HttpStatusCode.Redirect, request.StatusCode);
        Assert.Contains(
            WebAppRoutes.PasswordRecoveryPreview,
            request.Headers.Location!.OriginalString
        );

        using var preview = await Client.GetAsync(WebAppRoutes.PasswordRecoveryPreview);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);

        return Html.FirstTextArea(await preview.Content.ReadAsStringAsync());
    }
}

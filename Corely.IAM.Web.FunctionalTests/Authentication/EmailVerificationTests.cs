using System.Net;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.WebApp.EmailVerifications;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class EmailVerificationTests : FunctionalTestBase
{
    private const string USERNAME = "newcomer";
    private const string EMAIL = "newcomer@example.com";
    private const string PASSWORD = "Test1234!";

    protected override IReadOnlyDictionary<string, string> Settings =>
        new Dictionary<string, string> { ["SecurityOptions:RequireVerifiedEmail"] = "true" };

    private Guid _userId;

    private async Task RegisterNewcomerAsync()
    {
        var result = await Factory.WithScopeAsync(services =>
            services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(new RegisterUserRequest(USERNAME, EMAIL, PASSWORD))
        );
        Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);
        _userId = result.CreatedUserId;
    }

    private string LastLinkToken() =>
        Factory.Services.GetRequiredService<EmailVerificationPreviewStore>().Last!.Token;

    private async Task<string> SignInToTheWaitingPageAsync()
    {
        await RegisterNewcomerAsync();
        using var signIn = await SignInAsync(USERNAME, PASSWORD);
        Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
        Assert.Equal(AppRoutes.VerifyEmail, signIn.Headers.Location!.OriginalString);
        Assert.False(HasAuthCookies);

        using var page = await Client.GetAsync(AppRoutes.VerifyEmail);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        return await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostWaitingPageAsync(string handler, string html) =>
        Client.PostFormAsync(
            $"{AppRoutes.VerifyEmail}?handler={handler}",
            new Dictionary<string, string>
            {
                ["ChallengeToken"] = Html.InputValue(html, "ChallengeToken")!,
                ["ReturnUrl"] = Html.InputValue(html, "ReturnUrl") ?? string.Empty,
            },
            Html.InputValue(html, "__RequestVerificationToken")
        );

    [Fact]
    public async Task SignIn_LandsOnTheWaitingPage_ForAnUnconfirmedUser()
    {
        var html = await SignInToTheWaitingPageAsync();

        Assert.Contains(EMAIL, html);
        Assert.NotNull(LastLinkToken());
    }

    private async Task<string> OpenLinkAsync()
    {
        using var page = await Client.GetAsync(
            $"{AppRoutes.ConfirmEmail}?token={Uri.EscapeDataString(LastLinkToken())}",
            "cross-site"
        );
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("no-referrer", page.Headers.GetValues("Referrer-Policy").Single());
        return await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string> ChooseOnLinkAsync(string handler)
    {
        var html = await OpenLinkAsync();
        using var response = await Client.PostFormAsync(
            $"{AppRoutes.ConfirmEmail}?handler={handler}",
            new Dictionary<string, string> { ["Token"] = Html.InputValue(html, "Token")! },
            Html.InputValue(html, "__RequestVerificationToken")
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConfirmLink_ChangesNothing_WhenOnlyOpened()
    {
        await SignInToTheWaitingPageAsync();

        var html = await OpenLinkAsync();

        Assert.Contains("Confirm my email", html);
        Assert.Null((await GetUserRowAsync(_userId))!.EmailVerifiedUtc);
    }

    [Fact]
    public async Task ConfirmLink_Confirms_OnlyWhenChosen_AndSignsNobodyIn()
    {
        await SignInToTheWaitingPageAsync();

        var html = await ChooseOnLinkAsync("Confirm");

        Assert.Contains("Email confirmed", html);
        Assert.False(HasAuthCookies);
        Assert.NotNull((await GetUserRowAsync(_userId))!.EmailVerifiedUtc);
    }

    [Fact]
    public async Task ConfirmLink_RemovesTheSignUp_AndEndsItsWait_ForSomeoneWhoDidNotSignUp()
    {
        var waiting = await SignInToTheWaitingPageAsync();

        var html = await ChooseOnLinkAsync("Reject");
        using var response = await PostWaitingPageAsync("Continue", waiting);

        Assert.Contains("Account removed", html);
        Assert.Null(await GetUserRowAsync(_userId));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
        Assert.False(HasAuthCookies);
    }

    [Fact]
    public async Task WaitingPage_ContinuesSignedIn_OnceTheLinkIsOpened()
    {
        var html = await SignInToTheWaitingPageAsync();

        using (var early = await PostWaitingPageAsync("Continue", html))
        {
            Assert.Equal(HttpStatusCode.OK, early.StatusCode);
            Assert.False(HasAuthCookies);
        }

        await ChooseOnLinkAsync("Confirm");
        using var response = await PostWaitingPageAsync("Continue", html);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(HasAuthCookies);
    }

    [Fact]
    public async Task WaitingPage_ContinuesToTheReturnUrl_ForANewcomerWithNoAccounts()
    {
        const string returnUrl = "/accept-invitation?token=abc";
        await RegisterNewcomerAsync();
        using (
            var signIn = await Client.PostFormAsync(
                $"{AppRoutes.SignIn}?ReturnUrl={Uri.EscapeDataString(returnUrl)}",
                new Dictionary<string, string>
                {
                    ["Username"] = USERNAME,
                    ["Password"] = PASSWORD,
                    ["ReturnUrl"] = returnUrl,
                }
            )
        )
            Assert.Equal(AppRoutes.VerifyEmail, signIn.Headers.Location!.OriginalString);
        using var page = await Client.GetAsync(AppRoutes.VerifyEmail);
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        await ChooseOnLinkAsync("Confirm");
        using var response = await PostWaitingPageAsync("Continue", html);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Resend_WaitsOutTheCooldown()
    {
        var html = await SignInToTheWaitingPageAsync();
        var first = LastLinkToken();

        using (var early = await PostWaitingPageAsync("Resend", html))
            Assert.Equal(HttpStatusCode.OK, early.StatusCode);
        Assert.Equal(first, LastLinkToken());

        Clock.AdvanceSeconds(SecurityPolicy.EMAIL_VERIFICATION_RESEND_SECONDS_DEFAULT);
        using (var later = await PostWaitingPageAsync("Resend", html))
            Assert.Equal(HttpStatusCode.OK, later.StatusCode);
        Assert.NotEqual(first, LastLinkToken());
    }

    [Fact]
    public async Task WaitingPage_SendsBackToSignIn_WithoutAChallenge()
    {
        using var response = await Client.GetAsync(AppRoutes.VerifyEmail);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppRoutes.SignIn, response.Headers.Location!.OriginalString);
    }
}

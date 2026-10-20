using System.Net;
using Corely.IAM.Web.FunctionalTests.Infrastructure;
using Corely.IAM.WebApp;

namespace Corely.IAM.Web.FunctionalTests.Authentication;

public class ReturnUrlTests : FunctionalTestBase
{
    private const string OFF_SITE = "https://evil.example/accounts";

    private string AccountPage => WebAppRoutes.Users(AccountId);

    private static string Location(HttpResponseMessage response) =>
        response.Headers.Location!.OriginalString;

    private static void AssertStaysOnSite(HttpResponseMessage response)
    {
        if (response.Headers.Location is { } location)
            Assert.False(location.IsAbsoluteUri, $"Redirected off site to {location}");
    }

    private Task<HttpResponseMessage> SignInReturningToAsync(string returnUrl) =>
        Client.PostFormAsync(
            $"{AppRoutes.SignIn}?ReturnUrl={Uri.EscapeDataString(returnUrl)}",
            new Dictionary<string, string>
            {
                ["Username"] = SeedData.OwnerUsername,
                ["Password"] = SeedData.OwnerPassword,
                ["ReturnUrl"] = returnUrl,
            }
        );

    private async Task<HttpResponseMessage> VerifyMfaAsync(string secret)
    {
        using var challengePage = await Client.GetAsync(AppRoutes.VerifyMfa);
        var html = await challengePage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        return await Client.PostFormAsync(
            AppRoutes.VerifyMfa,
            new Dictionary<string, string>
            {
                ["Code"] = await GenerateTotpCodeAsync(secret),
                ["MfaChallengeToken"] = Html.InputValue(html, "MfaChallengeToken")!,
                ["ReturnUrl"] = Html.InputValue(html, "ReturnUrl") ?? string.Empty,
            },
            Html.InputValue(html, "__RequestVerificationToken")
        );
    }

    [Fact]
    public async Task AnonymousRequestToAnAccountPage_SignsInWithThePageAsReturnUrl()
    {
        using var response = await Client.GetAsync(AccountPage);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            $"ReturnUrl={Uri.EscapeDataString(AccountPage)}",
            Location(response),
            StringComparison.OrdinalIgnoreCase
        );
    }

    [Fact]
    public async Task SignIn_LandsOnTheReturnUrl()
    {
        using var response = await SignInReturningToAsync(AccountPage);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountPage, Location(response));
    }

    [Fact]
    public async Task SignInWithMfa_LandsOnTheReturnUrl()
    {
        var secret = await EnableMfaAsync();
        using (var signIn = await SignInReturningToAsync(AccountPage))
            Assert.Contains(AppRoutes.VerifyMfa, Location(signIn));

        using var response = await VerifyMfaAsync(secret);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountPage, Location(response));
    }

    [Fact]
    public async Task SwitchAccount_LandsOnTheReturnUrl_ForAnAccountTheUserCanEnter()
    {
        var secondAccountId = await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();
        var page = WebAppRoutes.Users(secondAccountId);

        using var response = await Client.GetAsync(
            $"{AppRoutes.SwitchAccount}?accountId={secondAccountId}&returnUrl={Uri.EscapeDataString(page)}",
            "same-origin"
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(page, Location(response));
    }

    [Fact]
    public async Task SwitchAccount_LandsOnAccountSelection_ForAnAccountTheUserCannotEnter()
    {
        await SignInSuccessfullyAsync();
        var otherAccountId = Guid.CreateVersion7();

        using var response = await Client.GetAsync(
            $"{AppRoutes.SwitchAccount}?accountId={otherAccountId}&returnUrl={Uri.EscapeDataString(WebAppRoutes.Users(otherAccountId))}",
            "same-origin"
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AppRoutes.SelectAccount, Location(response));
    }

    [Fact]
    public async Task SignIn_NeverRedirectsOffSite()
    {
        using var response = await SignInReturningToAsync(OFF_SITE);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertStaysOnSite(response);
    }

    [Fact]
    public async Task SignInPage_NeverRedirectsOffSite_WhenAlreadySignedIn()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(
            $"{AppRoutes.SignIn}?ReturnUrl={Uri.EscapeDataString(OFF_SITE)}"
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertStaysOnSite(response);
    }

    [Fact]
    public async Task SignInWithMfa_NeverRedirectsOffSite()
    {
        var secret = await EnableMfaAsync();
        using (var signIn = await SignInReturningToAsync(OFF_SITE))
            AssertStaysOnSite(signIn);

        using var response = await VerifyMfaAsync(secret);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertStaysOnSite(response);
    }

    [Fact]
    public async Task SwitchAccount_NeverRedirectsOffSite()
    {
        await SignInSuccessfullyAsync();

        using var response = await Client.GetAsync(
            $"{AppRoutes.SwitchAccount}?accountId={AccountId}&returnUrl={Uri.EscapeDataString(OFF_SITE)}",
            "same-origin"
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertStaysOnSite(response);
    }

    [Fact]
    public async Task SelectAccount_NeverRedirectsOffSite()
    {
        await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();

        using var response = await Client.PostFormAsync(
            AppRoutes.SelectAccount,
            new Dictionary<string, string>
            {
                ["accountId"] = AccountId.ToString(),
                ["ReturnUrl"] = OFF_SITE,
            }
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        AssertStaysOnSite(response);
    }

    [Fact]
    public async Task SelectAccount_LandsOnTheReturnUrl()
    {
        await CreateAdditionalAccountAsync(SeedData.SecondAccountName);
        await SignInSuccessfullyAsync();

        using var response = await Client.PostFormAsync(
            $"{AppRoutes.SelectAccount}?ReturnUrl={Uri.EscapeDataString(AccountPage)}",
            new Dictionary<string, string>
            {
                ["accountId"] = AccountId.ToString(),
                ["ReturnUrl"] = AccountPage,
            }
        );

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountPage, Location(response));
    }
}

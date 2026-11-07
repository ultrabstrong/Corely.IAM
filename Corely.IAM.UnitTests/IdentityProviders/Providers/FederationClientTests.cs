using System.Web;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.IdentityProviders.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests.IdentityProviders.Providers;

public sealed class FederationClientTests : IDisposable
{
    private const string CLIENT_ID = "client-id";
    private const string SECRET = "client-secret";
    private const string REDIRECT = "https://app.test/signin/callback/google";
    private const string GOOGLE_ISSUER = "https://accounts.google.com";
    private const string MICROSOFT_DISCOVERY =
        "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";
    private const string MICROSOFT_ISSUER_TEMPLATE =
        "https://login.microsoftonline.com/{tenantid}/v2.0";
    private const string TENANT_ID = "6a0e35c6-4dd3-4c34-8c2b-6c2a4f6a2b11";

    private readonly StubIdentityProvider _stub = new();
    private readonly FederationClient _client;
    private readonly DateTime _now = DateTime.UtcNow;

    public FederationClientTests()
    {
        _client = new FederationClient(
            new FederationHttp(new HttpClient(_stub)),
            NullLogger<FederationClient>.Instance
        );
    }

    public void Dispose() => _stub.Dispose();

    private static ResolvedIdentityProvider Provider(
        IdentityProviderType type,
        string? tenant = null
    ) =>
        new(
            new IdentityProviderSettings(
                type,
                true,
                true,
                CLIENT_ID,
                IdentityProviderCredentialKind.ClientSecret,
                true,
                null,
                tenant
            ),
            SECRET,
            null
        );

    private static FederatedSignInCorrelation Correlation(
        IdentityProviderType type,
        string? nonce = "nonce",
        string issuer = GOOGLE_ISSUER
    ) =>
        new(
            type,
            FederatedSignInPurpose.SignIn,
            "state",
            "verifier-verifier-verifier-verifier-verifier",
            nonce,
            issuer,
            DateTime.UtcNow
        );

    private void ServeGoogleToken(string idToken)
    {
        _stub.ServeDiscovery(GoogleProviderDefinition.DISCOVERY_URL, GOOGLE_ISSUER);
        _stub.Json("https://accounts.google.com/token", new { id_token = idToken });
    }

    private string GoogleToken(
        string? nonce = "nonce",
        string audience = CLIENT_ID,
        string issuer = GOOGLE_ISSUER,
        bool emailVerified = true,
        int lifetimeSeconds = 300
    ) =>
        _stub.IdToken(
            issuer,
            audience,
            "google-subject",
            nonce,
            _now,
            new Dictionary<string, object>
            {
                ["email"] = "person@gmail.com",
                ["email_verified"] = emailVerified,
            },
            lifetimeSeconds
        );

    private Task<ExternalIdentity?> RedeemAsync(
        IdentityProviderDefinition definition,
        FederatedSignInCorrelation correlation,
        string? tenant = null
    ) =>
        _client.RedeemAsync(
            definition,
            Provider(definition.Type, tenant),
            correlation,
            "the-code",
            REDIRECT,
            _now
        );

    [Fact]
    public async Task Authorize_BuildsACodeRequest_WithPkceStateAndNonce()
    {
        _stub.ServeDiscovery(GoogleProviderDefinition.DISCOVERY_URL, GOOGLE_ISSUER);

        var authorization = await _client.AuthorizeAsync(
            new GoogleProviderDefinition(),
            Provider(IdentityProviderType.Google),
            FederatedSignInPurpose.SignIn,
            REDIRECT,
            _now
        );

        Assert.NotNull(authorization);
        var url = new Uri(authorization.Url);
        var query = HttpUtility.ParseQueryString(url.Query);
        Assert.Equal("https://accounts.google.com/authorize", url.GetLeftPart(UriPartial.Path));
        Assert.Equal("code", query["response_type"]);
        Assert.Equal(CLIENT_ID, query["client_id"]);
        Assert.Equal(REDIRECT, query["redirect_uri"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.Equal(
            FederationClient.CodeChallenge(authorization.Correlation.CodeVerifier),
            query["code_challenge"]
        );
        Assert.Equal(authorization.Correlation.State, query["state"]);
        Assert.Equal(authorization.Correlation.Nonce, query["nonce"]);
        Assert.Equal(GOOGLE_ISSUER, authorization.Correlation.Issuer);
    }

    [Fact]
    public async Task Authorize_MakesAFreshStateVerifierAndNonce_EveryTime()
    {
        _stub.ServeDiscovery(GoogleProviderDefinition.DISCOVERY_URL, GOOGLE_ISSUER);
        var definition = new GoogleProviderDefinition();
        var provider = Provider(IdentityProviderType.Google);

        var first = await _client.AuthorizeAsync(
            definition,
            provider,
            FederatedSignInPurpose.SignIn,
            REDIRECT,
            _now
        );
        var second = await _client.AuthorizeAsync(
            definition,
            provider,
            FederatedSignInPurpose.SignIn,
            REDIRECT,
            _now
        );

        Assert.NotEqual(first!.Correlation.State, second!.Correlation.State);
        Assert.NotEqual(first.Correlation.CodeVerifier, second.Correlation.CodeVerifier);
        Assert.NotEqual(first.Correlation.Nonce, second.Correlation.Nonce);
    }

    [Fact]
    public async Task Authorize_ReturnsNothing_WhenDiscoveryCannotBeRead()
    {
        var authorization = await _client.AuthorizeAsync(
            new GoogleProviderDefinition(),
            Provider(IdentityProviderType.Google),
            FederatedSignInPurpose.SignIn,
            REDIRECT,
            _now
        );

        Assert.Null(authorization);
    }

    [Fact]
    public async Task Redeem_ReturnsTheIdentity_ForAValidIdToken()
    {
        ServeGoogleToken(GoogleToken());

        var identity = await RedeemAsync(
            new GoogleProviderDefinition(),
            Correlation(IdentityProviderType.Google)
        );

        Assert.NotNull(identity);
        Assert.Equal(GOOGLE_ISSUER, identity.Issuer);
        Assert.Equal("google-subject", identity.Subject);
        Assert.Equal("person@gmail.com", identity.Email);
        Assert.True(identity.EmailVerified);
        var body = _stub.RequestBodies.Last();
        Assert.Contains("code_verifier=verifier-verifier-verifier-verifier-verifier", body);
        Assert.Contains($"client_secret={SECRET}", body);
        Assert.Contains("grant_type=authorization_code", body);
    }

    [Fact]
    public async Task Redeem_ReadsAnUnverifiedEmail_AsUnverified()
    {
        ServeGoogleToken(GoogleToken(emailVerified: false));

        var identity = await RedeemAsync(
            new GoogleProviderDefinition(),
            Correlation(IdentityProviderType.Google)
        );

        Assert.False(identity!.EmailVerified);
    }

    [Fact]
    public async Task Redeem_Refuses_ATokenForAnotherAudience()
    {
        ServeGoogleToken(GoogleToken(audience: "someone-else"));

        Assert.Null(
            await RedeemAsync(
                new GoogleProviderDefinition(),
                Correlation(IdentityProviderType.Google)
            )
        );
    }

    [Fact]
    public async Task Redeem_Refuses_ATokenWithoutThisSignInsNonce()
    {
        ServeGoogleToken(GoogleToken(nonce: "another-nonce"));

        Assert.Null(
            await RedeemAsync(
                new GoogleProviderDefinition(),
                Correlation(IdentityProviderType.Google)
            )
        );
    }

    [Fact]
    public async Task Redeem_Refuses_AnExpiredToken()
    {
        ServeGoogleToken(GoogleToken(lifetimeSeconds: -(FederationClient.CLOCK_SKEW_SECONDS + 60)));

        Assert.Null(
            await RedeemAsync(
                new GoogleProviderDefinition(),
                Correlation(IdentityProviderType.Google)
            )
        );
    }

    [Fact]
    public async Task Redeem_Refuses_ATokenFromAnotherIssuer()
    {
        ServeGoogleToken(GoogleToken(issuer: "https://impostor.example"));

        Assert.Null(
            await RedeemAsync(
                new GoogleProviderDefinition(),
                Correlation(IdentityProviderType.Google)
            )
        );
    }

    [Fact]
    public async Task Redeem_ReturnsNothing_WhenTheCodeIsRefused()
    {
        _stub.ServeDiscovery(GoogleProviderDefinition.DISCOVERY_URL, GOOGLE_ISSUER);
        _stub.Json(
            "https://accounts.google.com/token",
            new { error = "invalid_grant" },
            System.Net.HttpStatusCode.BadRequest
        );

        Assert.Null(
            await RedeemAsync(
                new GoogleProviderDefinition(),
                Correlation(IdentityProviderType.Google)
            )
        );
    }

    private string MicrosoftToken(
        string issuerTenant,
        string tokenTenant,
        IDictionary<string, object>? extra = null
    )
    {
        var claims = new Dictionary<string, object>
        {
            ["tid"] = tokenTenant,
            ["email"] = "person@contoso.com",
        };
        foreach (var claim in extra ?? new Dictionary<string, object>())
            claims[claim.Key] = claim.Value;
        return _stub.IdToken(
            MICROSOFT_ISSUER_TEMPLATE.Replace("{tenantid}", issuerTenant),
            CLIENT_ID,
            "microsoft-subject",
            "nonce",
            _now,
            claims
        );
    }

    private void ServeMicrosoftToken(string idToken)
    {
        _stub.ServeDiscovery(
            MICROSOFT_DISCOVERY,
            MICROSOFT_ISSUER_TEMPLATE,
            MICROSOFT_ISSUER_TEMPLATE
        );
        _stub.Json("https://login.microsoftonline.com/token", new { id_token = idToken });
    }

    [Fact]
    public async Task Microsoft_AcceptsATenantIssuer_MatchingItsTid()
    {
        ServeMicrosoftToken(MicrosoftToken(TENANT_ID, TENANT_ID));

        var identity = await RedeemAsync(
            new MicrosoftProviderDefinition(),
            Correlation(IdentityProviderType.Microsoft, issuer: MICROSOFT_ISSUER_TEMPLATE)
        );

        Assert.NotNull(identity);
        Assert.Equal($"https://login.microsoftonline.com/{TENANT_ID}/v2.0", identity.Issuer);
        Assert.False(identity.EmailVerified);
    }

    [Fact]
    public async Task Microsoft_Refuses_AnIssuerForAnotherTenantThanItsTid()
    {
        ServeMicrosoftToken(MicrosoftToken(TENANT_ID, Guid.NewGuid().ToString()));

        Assert.Null(
            await RedeemAsync(
                new MicrosoftProviderDefinition(),
                Correlation(IdentityProviderType.Microsoft, issuer: MICROSOFT_ISSUER_TEMPLATE)
            )
        );
    }

    [Theory]
    [InlineData(MicrosoftProviderDefinition.PERSONAL_ACCOUNTS_TENANT_ID, false, true)]
    [InlineData(TENANT_ID, true, true)]
    [InlineData(TENANT_ID, false, false)]
    public async Task Microsoft_CountsTheEmailVerified_OnlyForPersonalAccountsOrXmsEdov(
        string tenant,
        bool xmsEdov,
        bool expected
    )
    {
        var extra = xmsEdov ? new Dictionary<string, object> { ["xms_edov"] = true } : null;
        ServeMicrosoftToken(MicrosoftToken(tenant, tenant, extra));

        var identity = await RedeemAsync(
            new MicrosoftProviderDefinition(),
            Correlation(IdentityProviderType.Microsoft, issuer: MICROSOFT_ISSUER_TEMPLATE)
        );

        Assert.Equal(expected, identity!.EmailVerified);
    }

    [Fact]
    public async Task Microsoft_AsksForCredentialsAgain_WhenReauthenticating()
    {
        _stub.ServeDiscovery(MICROSOFT_DISCOVERY, MICROSOFT_ISSUER_TEMPLATE);

        var authorization = await _client.AuthorizeAsync(
            new MicrosoftProviderDefinition(),
            Provider(IdentityProviderType.Microsoft),
            FederatedSignInPurpose.Reauthenticate,
            REDIRECT,
            _now
        );

        var query = HttpUtility.ParseQueryString(new Uri(authorization!.Url).Query);
        Assert.Equal("login", query["prompt"]);
        Assert.Equal("0", query["max_age"]);
    }

    [Fact]
    public async Task Microsoft_UsesTheConfiguredTenant()
    {
        _stub.ServeDiscovery(
            $"https://login.microsoftonline.com/{TENANT_ID}/v2.0/.well-known/openid-configuration",
            $"https://login.microsoftonline.com/{TENANT_ID}/v2.0"
        );

        var authorization = await _client.AuthorizeAsync(
            new MicrosoftProviderDefinition(),
            Provider(IdentityProviderType.Microsoft, TENANT_ID),
            FederatedSignInPurpose.SignIn,
            REDIRECT,
            _now
        );

        Assert.NotNull(authorization);
    }

    private void ServeGitHub(object emails)
    {
        _stub.Json(
            "https://github.com/login/oauth/access_token",
            new { access_token = "gh-token" }
        );
        _stub.Json(GitHubProviderDefinition.USER_URL, new { id = 42, login = "someone" });
        _stub.Json(GitHubProviderDefinition.EMAILS_URL, emails);
    }

    [Fact]
    public async Task GitHub_KeysOnTheNumericId_AndTakesThePrimaryVerifiedEmail()
    {
        ServeGitHub(
            new object[]
            {
                new
                {
                    email = "old@example.com",
                    primary = false,
                    verified = true,
                },
                new
                {
                    email = "main@example.com",
                    primary = true,
                    verified = true,
                },
            }
        );

        var identity = await RedeemAsync(
            new GitHubProviderDefinition(),
            Correlation(IdentityProviderType.GitHub, nonce: null, issuer: "https://github.com")
        );

        Assert.NotNull(identity);
        Assert.Equal("42", identity.Subject);
        Assert.Equal("https://github.com", identity.Issuer);
        Assert.Equal("main@example.com", identity.Email);
        Assert.True(identity.EmailVerified);
        Assert.Contains("code_verifier=", _stub.RequestBodies[0]);
    }

    [Fact]
    public async Task GitHub_HasNoEmail_WithoutAPrimaryVerifiedOne()
    {
        ServeGitHub(
            new object[]
            {
                new
                {
                    email = "main@example.com",
                    primary = true,
                    verified = false,
                },
            }
        );

        var identity = await RedeemAsync(
            new GitHubProviderDefinition(),
            Correlation(IdentityProviderType.GitHub, nonce: null, issuer: "https://github.com")
        );

        Assert.Null(identity!.Email);
        Assert.False(identity.EmailVerified);
    }

    private void ServeFacebook(string appId)
    {
        _stub.Json(
            $"{FacebookProviderDefinition.GRAPH_URL}/oauth/access_token",
            new { access_token = "fb-token" }
        );
        _stub.Json(
            $"{FacebookProviderDefinition.GRAPH_URL}/debug_token",
            new
            {
                data = new
                {
                    is_valid = true,
                    app_id = appId,
                    user_id = "777",
                },
            }
        );
        _stub.Json(
            $"{FacebookProviderDefinition.GRAPH_URL}/me",
            new { id = "777", email = "person@example.com" }
        );
    }

    [Fact]
    public async Task Facebook_ChecksTheTokenWasIssuedToThisApp_AndProvesTheSecret()
    {
        ServeFacebook(CLIENT_ID);

        var identity = await RedeemAsync(
            new FacebookProviderDefinition(),
            Correlation(
                IdentityProviderType.Facebook,
                nonce: null,
                issuer: "https://www.facebook.com"
            )
        );

        Assert.NotNull(identity);
        Assert.Equal("777", identity.Subject);
        Assert.True(identity.EmailVerified);
        var me = _stub.Requests.Single(r => r.RequestUri!.AbsolutePath.EndsWith("/me"));
        Assert.Equal(
            FacebookProviderDefinition.AppSecretProof("fb-token", SECRET),
            HttpUtility.ParseQueryString(me.RequestUri!.Query)["appsecret_proof"]
        );
    }

    [Fact]
    public async Task Facebook_Refuses_ATokenIssuedToAnotherApp()
    {
        ServeFacebook("another-app");

        Assert.Null(
            await RedeemAsync(
                new FacebookProviderDefinition(),
                Correlation(
                    IdentityProviderType.Facebook,
                    nonce: null,
                    issuer: "https://www.facebook.com"
                )
            )
        );
    }

    [Fact]
    public async Task Amazon_ReadsTheUserIdAndEmail_FromTheProfile()
    {
        _stub.Json("https://api.amazon.com/auth/o2/token", new { access_token = "amzn-token" });
        _stub.Json(
            AmazonProviderDefinition.PROFILE_URL,
            new { user_id = "amzn1.account.ABC", email = "person@example.com" }
        );

        var identity = await RedeemAsync(
            new AmazonProviderDefinition(),
            Correlation(IdentityProviderType.Amazon, nonce: null, issuer: "https://www.amazon.com")
        );

        Assert.Equal("amzn1.account.ABC", identity!.Subject);
        Assert.Equal("https://www.amazon.com", identity.Issuer);
        Assert.True(identity.EmailVerified);
    }
}

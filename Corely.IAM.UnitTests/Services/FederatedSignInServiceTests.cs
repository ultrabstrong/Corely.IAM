using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.IdentityProviders.Types;
using Corely.IAM.Security.Entities;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests.Services;

public class FederatedSignInServiceTests
{
    private const string REDIRECT = "https://app.test/signin/callback/google";
    private const string GOOGLE_ISSUER = "https://accounts.google.com";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IIdentityProviderSettingsProvider> _settings = new();
    private readonly Mock<IFederationClient> _client = new();
    private readonly Mock<IFederatedIdentityProcessor> _identities = new();
    private readonly Mock<IUserContextProvider> _userContext = new();
    private readonly FederatedSignInService _service;
    private IdentityProviderSettings _google = Settings(IdentityProviderType.Google);

    public FederatedSignInServiceTests()
    {
        _settings
            .Setup(s => s.GetUsableAsync(It.IsAny<IdentityProviderType>()))
            .ReturnsAsync(
                (IdentityProviderType type) =>
                    type == IdentityProviderType.Google
                        ? new ResolvedIdentityProvider(_google, "secret", null)
                        : new ResolvedIdentityProvider(Settings(type), "secret", null)
            );
        _identities
            .Setup(i => i.LinkToNewUserAsync(It.IsAny<Guid>(), It.IsAny<ExternalIdentity>()))
            .ReturnsAsync(
                new LinkFederatedIdentityResult(LinkFederatedIdentityResultCode.Success, "")
            );

        _service = new FederatedSignInService(
            _settings.Object,
            new IdentityProviderCatalog(),
            _client.Object,
            _identities.Object,
            _serviceFactory.GetRequiredService<IUserProcessor>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AuthTokenEntity>>(),
            _serviceFactory.GetRequiredService<AuthenticationService>(),
            _userContext.Object,
            _serviceFactory.GetRequiredService<IUnitOfWorkProvider>(),
            TimeProvider.System,
            NullLogger<FederatedSignInService>.Instance
        );
    }

    private static IdentityProviderSettings Settings(
        IdentityProviderType type,
        bool allowSignUp = true
    ) =>
        new(
            type,
            true,
            allowSignUp,
            "client-id",
            IdentityProviderCredentialKind.ClientSecret,
            true,
            null,
            null
        );

    private static FederatedSignInCorrelation Correlation(
        FederatedSignInPurpose purpose = FederatedSignInPurpose.SignIn,
        IdentityProviderType type = IdentityProviderType.Google,
        DateTime? startedUtc = null
    ) =>
        new(
            type,
            purpose,
            "the-state",
            "verifier",
            "nonce",
            GOOGLE_ISSUER,
            startedUtc ?? DateTime.UtcNow
        );

    private static CompleteFederatedSignInRequest Request(
        FederatedSignInCorrelation? correlation = null,
        string? state = "the-state",
        string? issuer = null,
        string? error = null,
        string? code = "the-code"
    ) => new(correlation ?? Correlation(), REDIRECT, "device", code, state, issuer, error);

    private void Returns(ExternalIdentity? identity) =>
        _client
            .Setup(c =>
                c.RedeemAsync(
                    It.IsAny<IdentityProviderDefinition>(),
                    It.IsAny<ResolvedIdentityProvider>(),
                    It.IsAny<FederatedSignInCorrelation>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()
                )
            )
            .ReturnsAsync(identity);

    private static ExternalIdentity Identity(
        string? email = "new.person@example.com",
        bool verified = true,
        IdentityProviderType type = IdentityProviderType.Google,
        DateTime? authTime = null
    ) => new(type, GOOGLE_ISSUER, "subject", email, verified, authTime);

    private void SignedIn(Guid userId, Guid? tokenId = null) =>
        _userContext
            .Setup(u => u.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = userId,
                        Username = "someone",
                        Email = "someone@test.com",
                    },
                    null,
                    "device",
                    [],
                    tokenId
                )
            );

    private async Task<Guid> CreateUserAsync(string username, string email) =>
        (
            await _serviceFactory
                .GetRequiredService<IUserProcessor>()
                .CreateUserAsync(new(username, email))
        ).CreatedId;

    [Fact]
    public async Task Complete_RefusesAnExpiredSignIn()
    {
        var result = await _service.CompleteFederatedSignInAsync(
            Request(
                Correlation(
                    startedUtc: DateTime.UtcNow.AddSeconds(
                        -FederatedSignInCorrelation.MAX_AGE_SECONDS - 1
                    )
                )
            )
        );

        Assert.Equal(CompleteFederatedSignInResultCode.ExpiredError, result.ResultCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("another-state")]
    public async Task Complete_RefusesAMismatchedState_WithoutRedeemingTheCode(string? state)
    {
        var result = await _service.CompleteFederatedSignInAsync(Request(state: state));

        Assert.Equal(CompleteFederatedSignInResultCode.InvalidStateError, result.ResultCode);
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_RefusesAResponseFromAnotherIssuer()
    {
        var result = await _service.CompleteFederatedSignInAsync(
            Request(issuer: "https://impostor.example")
        );

        Assert.Equal(CompleteFederatedSignInResultCode.IssuerMismatchError, result.ResultCode);
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_ChangesNothing_WhenTheProviderReturnsAnError()
    {
        var result = await _service.CompleteFederatedSignInAsync(
            Request(error: "access_denied", code: null)
        );

        Assert.Equal(CompleteFederatedSignInResultCode.ProviderDeclinedError, result.ResultCode);
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Complete_RefusesAProviderThatIsNotAvailable()
    {
        _settings
            .Setup(s => s.GetUsableAsync(IdentityProviderType.Google))
            .ReturnsAsync((ResolvedIdentityProvider?)null);

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(
            CompleteFederatedSignInResultCode.ProviderNotAvailableError,
            result.ResultCode
        );
    }

    [Fact]
    public async Task Complete_RefusesAResponseThatCouldNotBeVerified()
    {
        Returns(null);

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.InvalidResponseError, result.ResultCode);
    }

    [Fact]
    public async Task SignIn_SignsInTheLinkedUser()
    {
        var userId = await CreateUserAsync("linked", "linked@example.com");
        Returns(Identity(email: "anything@example.com", verified: false));
        _identities.Setup(i => i.FindUserIdAsync(GOOGLE_ISSUER, "subject")).ReturnsAsync(userId);

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.Success, result.ResultCode);
        Assert.False(result.CreatedUser);
        Assert.NotNull(result.SignIn!.AuthToken);
    }

    [Fact]
    public async Task SignIn_CreatesAUser_FromAVerifiedEmail_WhenSignUpIsAllowed()
    {
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.Success, result.ResultCode);
        Assert.True(result.CreatedUser);
        var user = await _serviceFactory
            .GetRequiredService<IReadonlyRepo<UserEntity>>()
            .GetAsync(u => u.Email == "new.person@example.com");
        Assert.Equal("new.person", user!.Username);
        _identities.Verify(
            i => i.LinkToNewUserAsync(user.Id, It.IsAny<ExternalIdentity>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SignIn_AddsASuffix_WhenTheUsernameIsTaken()
    {
        await CreateUserAsync("new.person", "someone.else@example.com");
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.True(result.CreatedUser);
        var user = await _serviceFactory
            .GetRequiredService<IReadonlyRepo<UserEntity>>()
            .GetAsync(u => u.Email == "new.person@example.com");
        Assert.StartsWith("new.person-", user!.Username);
    }

    [Fact]
    public async Task SignIn_CreatesNoUser_WhenSignUpIsOff()
    {
        _google = Settings(IdentityProviderType.Google, allowSignUp: false);
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.NotLinkedError, result.ResultCode);
        Assert.False(
            await _serviceFactory
                .GetRequiredService<IReadonlyRepo<UserEntity>>()
                .AnyAsync(u => u.Email == "new.person@example.com")
        );
    }

    [Fact]
    public async Task SignIn_CreatesNoUser_WithoutAnEmail()
    {
        Returns(Identity(email: null, verified: false));

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.NoEmailError, result.ResultCode);
    }

    [Fact]
    public async Task SignIn_CreatesNoUser_FromAnEmailTheProviderCallsUnverified()
    {
        Returns(Identity(verified: false));

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.EmailNotVerifiedError, result.ResultCode);
    }

    [Fact]
    public async Task SignIn_NeverJoinsAnExistingUserByEmail()
    {
        await CreateUserAsync("existing", "new.person@example.com");
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(Request());

        Assert.Equal(CompleteFederatedSignInResultCode.EmailInUseError, result.ResultCode);
        _identities.Verify(
            i => i.LinkToNewUserAsync(It.IsAny<Guid>(), It.IsAny<ExternalIdentity>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Link_NeedsASignedInUser()
    {
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(
            Request(Correlation(FederatedSignInPurpose.Link))
        );

        Assert.Equal(CompleteFederatedSignInResultCode.UnauthorizedError, result.ResultCode);
    }

    [Theory]
    [InlineData(LinkFederatedIdentityResultCode.Success, CompleteFederatedSignInResultCode.Success)]
    [InlineData(
        LinkFederatedIdentityResultCode.AlreadyLinkedError,
        CompleteFederatedSignInResultCode.AlreadyLinkedError
    )]
    [InlineData(
        LinkFederatedIdentityResultCode.IdentityInUseError,
        CompleteFederatedSignInResultCode.IdentityInUseError
    )]
    public async Task Link_LinksToTheSignedInUser(
        Enum linkCode,
        CompleteFederatedSignInResultCode expected
    )
    {
        var userId = Guid.CreateVersion7();
        SignedIn(userId);
        Returns(Identity());
        _identities
            .Setup(i => i.LinkAsync(userId, It.IsAny<ExternalIdentity>()))
            .ReturnsAsync(
                new LinkFederatedIdentityResult((LinkFederatedIdentityResultCode)linkCode, "")
            );

        var result = await _service.CompleteFederatedSignInAsync(
            Request(Correlation(FederatedSignInPurpose.Link))
        );

        Assert.Equal(expected, result.ResultCode);
    }

    private async Task<(Guid UserId, Guid SessionId)> SignedInWithSessionAsync()
    {
        var userId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        await _serviceFactory
            .GetRequiredService<IRepo<AuthTokenEntity>>()
            .CreateAsync(
                new AuthTokenEntity
                {
                    Id = tokenId,
                    UserId = userId,
                    DeviceId = "device",
                    SessionId = sessionId,
                    RefreshSecretHash = "hash",
                }
            );
        SignedIn(userId, tokenId);
        return (userId, sessionId);
    }

    [Fact]
    public async Task Reauthenticate_RecordsAProofForThisSession()
    {
        var (userId, sessionId) = await SignedInWithSessionAsync();
        Returns(Identity());
        _identities
            .Setup(i =>
                i.RecordReauthenticationAsync(userId, It.IsAny<ExternalIdentity>(), sessionId)
            )
            .ReturnsAsync(true);

        var result = await _service.CompleteFederatedSignInAsync(
            Request(Correlation(FederatedSignInPurpose.Reauthenticate))
        );

        Assert.Equal(CompleteFederatedSignInResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task Reauthenticate_RefusesAnAccountNotLinkedToTheUser()
    {
        await SignedInWithSessionAsync();
        Returns(Identity());

        var result = await _service.CompleteFederatedSignInAsync(
            Request(Correlation(FederatedSignInPurpose.Reauthenticate))
        );

        Assert.Equal(CompleteFederatedSignInResultCode.NotLinkedToUserError, result.ResultCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-3600)]
    public async Task Reauthenticate_NeedsAFreshAuthTime_FromAProviderThatCanForceOne(
        int? ageSeconds
    )
    {
        var (userId, _) = await SignedInWithSessionAsync();
        Returns(
            Identity(
                type: IdentityProviderType.Microsoft,
                authTime: ageSeconds is { } age ? DateTime.UtcNow.AddSeconds(age) : null
            )
        );
        _identities
            .Setup(i =>
                i.RecordReauthenticationAsync(
                    userId,
                    It.IsAny<ExternalIdentity>(),
                    It.IsAny<Guid>()
                )
            )
            .ReturnsAsync(true);

        var result = await _service.CompleteFederatedSignInAsync(
            Request(
                Correlation(FederatedSignInPurpose.Reauthenticate, IdentityProviderType.Microsoft)
            )
        );

        Assert.Equal(CompleteFederatedSignInResultCode.InvalidResponseError, result.ResultCode);
        _identities.Verify(
            i =>
                i.RecordReauthenticationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ExternalIdentity>(),
                    It.IsAny<Guid>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Start_RefusesLinking_WithoutASignedInUser()
    {
        var result = await _service.StartFederatedSignInAsync(
            new StartFederatedSignInRequest(
                IdentityProviderType.Google,
                FederatedSignInPurpose.Link,
                REDIRECT
            )
        );

        Assert.Equal(StartFederatedSignInResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task Start_ReturnsTheAuthorizationUrlAndCorrelation()
    {
        var correlation = Correlation();
        _client
            .Setup(c =>
                c.AuthorizeAsync(
                    It.IsAny<IdentityProviderDefinition>(),
                    It.IsAny<ResolvedIdentityProvider>(),
                    FederatedSignInPurpose.SignIn,
                    REDIRECT,
                    It.IsAny<DateTime>()
                )
            )
            .ReturnsAsync(
                new FederationAuthorization("https://idp.example/authorize", correlation)
            );

        var result = await _service.StartFederatedSignInAsync(
            new StartFederatedSignInRequest(
                IdentityProviderType.Google,
                FederatedSignInPurpose.SignIn,
                REDIRECT
            )
        );

        Assert.Equal(StartFederatedSignInResultCode.Success, result.ResultCode);
        Assert.Equal("https://idp.example/authorize", result.AuthorizationUrl);
        Assert.Same(correlation, result.Correlation);
    }

    [Fact]
    public async Task ListIdentityProviders_ListsOnlyUsableOnes()
    {
        _settings
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync([
                Settings(IdentityProviderType.GitHub, allowSignUp: false),
                Settings(IdentityProviderType.Google) with
                {
                    Enabled = false,
                },
            ]);

        var providers = await _service.ListIdentityProvidersAsync();

        var only = Assert.Single(providers);
        Assert.Equal(IdentityProviderType.GitHub, only.Type);
        Assert.Equal("GitHub", only.DisplayName);
        Assert.False(only.AllowsSignUp);
    }
}

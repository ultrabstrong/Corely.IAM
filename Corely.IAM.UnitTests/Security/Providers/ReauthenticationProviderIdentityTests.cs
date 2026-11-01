using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Security.Providers;

public class ReauthenticationProviderIdentityTests
{
    private const string SUBJECT = "google-subject";
    private const string NONCE = "nonce-for-this-attempt";
    private const string ID_TOKEN = "id-token";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock = new();
    private readonly Mock<IGoogleIdTokenValidator> _validatorMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly SecurityOptions _securityOptions = new()
    {
        GoogleClientId = "client-id",
        LockoutCooldownSeconds = 60,
    };
    private readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ReauthenticationProvider _provider;

    public ReauthenticationProviderIdentityTests()
    {
        _timeProviderMock.Setup(t => t.GetUtcNow()).Returns(new DateTimeOffset(_now));
        _basicAuthProcessorMock
            .Setup(m => m.VerifyBasicAuthAsync(It.IsAny<VerifyBasicAuthRequest>()))
            .ReturnsAsync(
                (VerifyBasicAuthRequest r) =>
                    new VerifyBasicAuthResult(
                        VerifyBasicAuthResultCode.Success,
                        string.Empty,
                        r.Password == "correct"
                    )
            );
        _provider = new ReauthenticationProvider(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _basicAuthProcessorMock.Object,
            new Mock<ITotpAuthProcessor>().Object,
            FixedSecurityPolicyProvider.From(_securityOptions),
            _timeProviderMock.Object,
            _serviceFactory.GetRequiredService<IReadonlyRepo<BasicAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<GoogleAuthEntity>>(),
            _validatorMock.Object,
            Options.Create(_securityOptions)
        );
    }

    private async Task GiveUserAsync(
        bool password = false,
        bool google = true,
        DateTime? lockedUtc = null
    )
    {
        await _serviceFactory
            .GetRequiredService<IRepo<UserEntity>>()
            .CreateAsync(
                new UserEntity
                {
                    Id = _userId,
                    Username = "someone",
                    Email = "someone@test.com",
                    LockedUtc = lockedUtc,
                }
            );
        if (password)
            await _serviceFactory
                .GetRequiredService<IRepo<BasicAuthEntity>>()
                .CreateAsync(
                    new BasicAuthEntity
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = _userId,
                        Password = "hash",
                    }
                );
        if (google)
            await _serviceFactory
                .GetRequiredService<IRepo<GoogleAuthEntity>>()
                .CreateAsync(
                    new GoogleAuthEntity
                    {
                        Id = Guid.CreateVersion7(),
                        UserId = _userId,
                        GoogleSubjectId = SUBJECT,
                        Email = "someone@gmail.com",
                    }
                );
    }

    private void GoogleAnswers(
        string subject = SUBJECT,
        string? nonce = NONCE,
        DateTime? issuedUtc = null
    ) =>
        _validatorMock
            .Setup(x => x.ValidateAsync(ID_TOKEN))
            .ReturnsAsync(
                new GoogleIdTokenPayload(
                    subject,
                    "someone@gmail.com",
                    true,
                    nonce,
                    issuedUtc ?? _now
                )
            );

    private Task<ReauthenticationResult> VerifyAsync(string? idToken = ID_TOKEN) =>
        _provider.VerifyIdentityAsync(_userId, null, idToken, NONCE);

    [Fact]
    public async Task VerifyIdentity_Succeeds_ForAFreshGoogleSignInOfTheLinkedAccount()
    {
        await GiveUserAsync();
        GoogleAnswers();

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_AGoogleSignInOfAnotherAccount()
    {
        await GiveUserAsync();
        GoogleAnswers(subject: "someone-else");

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_AGoogleTokenWithAnotherNonce()
    {
        await GiveUserAsync();
        GoogleAnswers(nonce: "from-an-earlier-attempt");

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_AGoogleTokenWithNoNonce()
    {
        await GiveUserAsync();
        GoogleAnswers(nonce: null);

        var result = await _provider.VerifyIdentityAsync(_userId, null, ID_TOKEN, null);

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_AStaleGoogleToken_EvenThoughGoogleStillAcceptsIt()
    {
        await GiveUserAsync();
        GoogleAnswers(
            issuedUtc: _now.AddSeconds(-GoogleIdTokenPayload.REAUTHENTICATION_MAX_AGE_SECONDS - 1)
        );

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_AGoogleTokenWithNoIssuedTime()
    {
        await GiveUserAsync();
        _validatorMock
            .Setup(x => x.ValidateAsync(ID_TOKEN))
            .ReturnsAsync(new GoogleIdTokenPayload(SUBJECT, "someone@gmail.com", true, NONCE));

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_ATokenGoogleDoesNotVouchFor()
    {
        await GiveUserAsync();

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidGoogleSignInError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_AsksForAGoogleSignIn_WhenNoneIsGiven_SoASessionNeverCounts()
    {
        await GiveUserAsync();

        var result = await VerifyAsync(idToken: null);

        Assert.Equal(ReauthenticationResultCode.GoogleSignInRequiredError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Refuses_ALockedUser()
    {
        await GiveUserAsync(lockedUtc: _now);
        GoogleAnswers();

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.UserLockedError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_IgnoresAGoogleProof_ForAUserWithAPassword()
    {
        await GiveUserAsync(password: true);
        GoogleAnswers();

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.InvalidCurrentPasswordError, result.ResultCode);
        _validatorMock.Verify(x => x.ValidateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyIdentity_ChecksThePassword_ForAUserWithAPassword()
    {
        await GiveUserAsync(password: true);

        var result = await _provider.VerifyIdentityAsync(_userId, "correct", null, null);

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_ExplainsWhatToDo_ForAUserWithNoMethod()
    {
        await GiveUserAsync(google: false);

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.NoReauthenticationMethodError, result.ResultCode);
        Assert.Contains("Set a password on your profile", result.Message);
    }

    [Fact]
    public async Task VerifyIdentity_ExplainsWhatToDo_WhenGoogleIsNotConfigured()
    {
        _securityOptions.GoogleClientId = null;
        await GiveUserAsync();
        GoogleAnswers();

        var result = await VerifyAsync();

        Assert.Equal(ReauthenticationResultCode.NoReauthenticationMethodError, result.ResultCode);
    }
}

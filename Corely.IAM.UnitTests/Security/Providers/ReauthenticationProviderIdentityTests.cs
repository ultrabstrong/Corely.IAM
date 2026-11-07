using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Security.Providers;

public class ReauthenticationProviderIdentityTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IBasicAuthProcessor> _basicAuthProcessorMock = new();
    private readonly Mock<ITotpAuthProcessor> _totpAuthProcessorMock = new();
    private readonly Mock<IFederatedIdentityProcessor> _federatedIdentityProcessorMock = new();
    private readonly Mock<IUserContextProvider> _userContextProviderMock = new();
    private readonly Mock<TimeProvider> _timeProviderMock = new();
    private readonly SecurityOptions _securityOptions = new() { LockoutCooldownSeconds = 60 };
    private readonly DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Guid _tokenId = Guid.CreateVersion7();
    private readonly Guid _sessionId = Guid.CreateVersion7();
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
            _totpAuthProcessorMock.Object,
            FixedSecurityPolicyProvider.From(_securityOptions),
            _timeProviderMock.Object,
            _serviceFactory.GetRequiredService<IReadonlyRepo<BasicAuthEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AuthTokenEntity>>(),
            _federatedIdentityProcessorMock.Object,
            _userContextProviderMock.Object
        );
    }

    private async Task GiveUserAsync(
        bool password = false,
        bool linked = true,
        DateTime? lockedUtc = null,
        bool session = true
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
        {
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
        }
        _federatedIdentityProcessorMock.Setup(m => m.HasAnyAsync(_userId)).ReturnsAsync(linked);

        if (!session)
            return;
        await _serviceFactory
            .GetRequiredService<IRepo<AuthTokenEntity>>()
            .CreateAsync(
                new AuthTokenEntity
                {
                    Id = _tokenId,
                    UserId = _userId,
                    DeviceId = "device",
                    SessionId = _sessionId,
                    RefreshSecretHash = "hash",
                }
            );
        _userContextProviderMock
            .Setup(m => m.GetUserContext())
            .Returns(
                new UserContext(
                    new User
                    {
                        Id = _userId,
                        Username = "someone",
                        Email = "someone@test.com",
                    },
                    null,
                    "device",
                    [],
                    _tokenId
                )
            );
    }

    [Fact]
    public async Task VerifyIdentity_UsesThePassword_WhenTheUserHasOne()
    {
        await GiveUserAsync(password: true);

        var correct = await _provider.VerifyIdentityAsync(_userId, "correct");
        var wrong = await _provider.VerifyIdentityAsync(_userId, "wrong");

        Assert.Equal(ReauthenticationResultCode.Success, correct.ResultCode);
        Assert.Equal(ReauthenticationResultCode.InvalidCurrentPasswordError, wrong.ResultCode);
        _federatedIdentityProcessorMock.Verify(
            m => m.ConsumeReauthenticationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyIdentity_ReportsNoMethod_WithNoPasswordAndNoLinkedIdentity()
    {
        await GiveUserAsync(linked: false);

        var result = await _provider.VerifyIdentityAsync(_userId, null);

        Assert.Equal(ReauthenticationResultCode.NoReauthenticationMethodError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_RefusesALockedUser()
    {
        await GiveUserAsync(lockedUtc: _now);

        var result = await _provider.VerifyIdentityAsync(_userId, null);

        Assert.Equal(ReauthenticationResultCode.UserLockedError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_Succeeds_WhenThisSessionHasAFreshProof()
    {
        await GiveUserAsync();
        _federatedIdentityProcessorMock
            .Setup(m => m.ConsumeReauthenticationAsync(_userId, _sessionId))
            .ReturnsAsync(true);

        var result = await _provider.VerifyIdentityAsync(_userId, null);

        Assert.Equal(ReauthenticationResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_AsksForAFederatedSignIn_WhenThereIsNoProof()
    {
        await GiveUserAsync();
        _federatedIdentityProcessorMock
            .Setup(m => m.ConsumeReauthenticationAsync(_userId, _sessionId))
            .ReturnsAsync(false);

        var result = await _provider.VerifyIdentityAsync(_userId, null);

        Assert.Equal(ReauthenticationResultCode.FederatedSignInRequiredError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyIdentity_AsksForAFederatedSignIn_WithoutASession()
    {
        await GiveUserAsync(session: false);

        var result = await _provider.VerifyIdentityAsync(_userId, null);

        Assert.Equal(ReauthenticationResultCode.FederatedSignInRequiredError, result.ResultCode);
        _federatedIdentityProcessorMock.Verify(
            m => m.ConsumeReauthenticationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never
        );
    }

    [Fact]
    public async Task VerifyAccountKeyProof_ChecksTotpAfterTheProof()
    {
        await GiveUserAsync(password: true);
        _totpAuthProcessorMock.Setup(m => m.IsTotpEnabledAsync(_userId)).ReturnsAsync(true);

        var result = await _provider.VerifyAccountKeyProofAsync(
            _userId,
            new AccountKeyProof("correct", null)
        );

        Assert.Equal(ReauthenticationResultCode.TotpRequiredError, result.ResultCode);
    }
}

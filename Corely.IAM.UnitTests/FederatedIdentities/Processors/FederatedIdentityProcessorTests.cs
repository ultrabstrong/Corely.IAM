using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.FederatedIdentities.Entities;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests.FederatedIdentities.Processors;

public class FederatedIdentityProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IIdentityProviderSettingsProvider> _settings = new();
    private readonly Mock<TimeProvider> _time = new();
    private readonly FederatedIdentityProcessor _processor;
    private readonly Guid _userId = Guid.CreateVersion7();
    private DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private List<IdentityProviderSettings> _usable =
    [
        Usable(IdentityProviderType.Google),
        Usable(IdentityProviderType.GitHub),
    ];

    public FederatedIdentityProcessorTests()
    {
        _time.Setup(t => t.GetUtcNow()).Returns(() => new DateTimeOffset(_now));
        _settings.Setup(s => s.GetAllAsync()).ReturnsAsync(() => _usable);
        _processor = new FederatedIdentityProcessor(
            _serviceFactory.GetRequiredService<IRepo<FederatedIdentityEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<BasicAuthEntity>>(),
            _settings.Object,
            _time.Object,
            NullLogger<FederatedIdentityProcessor>.Instance
        );
    }

    private static IdentityProviderSettings Usable(IdentityProviderType type) =>
        new(
            type,
            true,
            true,
            "client",
            IdentityProviderCredentialKind.ClientSecret,
            true,
            null,
            null
        );

    private static ExternalIdentity Identity(
        IdentityProviderType type = IdentityProviderType.Google,
        string subject = "subject"
    ) => new(type, $"https://{type}.example", subject, "person@example.com", true);

    private Task GivePasswordAsync(Guid userId) =>
        _serviceFactory
            .GetRequiredService<IRepo<BasicAuthEntity>>()
            .CreateAsync(
                new BasicAuthEntity
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    Password = "hash",
                }
            );

    [Fact]
    public async Task Link_AddsTheIdentity_AndFindsItsUserByIssuerAndSubject()
    {
        var result = await _processor.LinkAsync(_userId, Identity());

        Assert.Equal(LinkFederatedIdentityResultCode.Success, result.ResultCode);
        Assert.Equal(
            _userId,
            await _processor.FindUserIdAsync("https://Google.example", "subject")
        );
        Assert.Null(await _processor.FindUserIdAsync("https://GitHub.example", "subject"));
    }

    [Fact]
    public async Task Link_RefusesAnIdentityAnotherUserHas()
    {
        await _processor.LinkAsync(Guid.CreateVersion7(), Identity());

        var result = await _processor.LinkAsync(_userId, Identity());

        Assert.Equal(LinkFederatedIdentityResultCode.IdentityInUseError, result.ResultCode);
    }

    [Fact]
    public async Task Link_AllowsOneIdentityPerProviderPerUser()
    {
        await _processor.LinkAsync(_userId, Identity(subject: "first"));

        var result = await _processor.LinkAsync(_userId, Identity(subject: "second"));

        Assert.Equal(LinkFederatedIdentityResultCode.AlreadyLinkedError, result.ResultCode);
    }

    [Fact]
    public async Task Unlink_RefusesTheLastWayToSignIn()
    {
        await _processor.LinkAsync(_userId, Identity());

        var result = await _processor.UnlinkAsync(_userId, IdentityProviderType.Google);

        Assert.Equal(UnlinkFederatedIdentityResultCode.LastAuthMethodError, result.ResultCode);
    }

    [Fact]
    public async Task Unlink_Succeeds_WhenThePasswordRemains()
    {
        await _processor.LinkAsync(_userId, Identity());
        await GivePasswordAsync(_userId);

        var result = await _processor.UnlinkAsync(_userId, IdentityProviderType.Google);

        Assert.Equal(UnlinkFederatedIdentityResultCode.Success, result.ResultCode);
        Assert.Empty(await _processor.ListAsync(_userId));
    }

    [Fact]
    public async Task Unlink_Succeeds_WhenAnotherUsableIdentityRemains()
    {
        await _processor.LinkAsync(_userId, Identity());
        await _processor.LinkAsync(_userId, Identity(IdentityProviderType.GitHub));

        var result = await _processor.UnlinkAsync(_userId, IdentityProviderType.Google);

        Assert.Equal(UnlinkFederatedIdentityResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task Unlink_DoesNotCountAnIdentityWhoseProviderIsOff()
    {
        await _processor.LinkAsync(_userId, Identity());
        await _processor.LinkAsync(_userId, Identity(IdentityProviderType.GitHub));
        _usable = [Usable(IdentityProviderType.Google)];

        var result = await _processor.UnlinkAsync(_userId, IdentityProviderType.Google);

        Assert.Equal(UnlinkFederatedIdentityResultCode.LastAuthMethodError, result.ResultCode);
    }

    [Fact]
    public async Task Unlink_ReportsNotLinked()
    {
        var result = await _processor.UnlinkAsync(_userId, IdentityProviderType.Amazon);

        Assert.Equal(UnlinkFederatedIdentityResultCode.NotLinkedError, result.ResultCode);
    }

    [Fact]
    public async Task Reauthentication_IsConsumedOnce_ByTheSessionThatMadeIt()
    {
        var sessionId = Guid.CreateVersion7();
        await _processor.LinkAsync(_userId, Identity());
        Assert.True(await _processor.RecordReauthenticationAsync(_userId, Identity(), sessionId));

        Assert.False(await _processor.ConsumeReauthenticationAsync(_userId, Guid.CreateVersion7()));
        Assert.True(await _processor.ConsumeReauthenticationAsync(_userId, sessionId));
        Assert.False(await _processor.ConsumeReauthenticationAsync(_userId, sessionId));
    }

    [Fact]
    public async Task Reauthentication_Expires()
    {
        var sessionId = Guid.CreateVersion7();
        await _processor.LinkAsync(_userId, Identity());
        await _processor.RecordReauthenticationAsync(_userId, Identity(), sessionId);
        _now = _now.AddSeconds(FederatedIdentityEntity.REAUTHENTICATION_MAX_AGE_SECONDS + 1);

        Assert.False(await _processor.ConsumeReauthenticationAsync(_userId, sessionId));
    }

    [Fact]
    public async Task Reauthentication_IsNotRecorded_ForAnIdentityNotLinkedToTheUser()
    {
        await _processor.LinkAsync(Guid.CreateVersion7(), Identity());

        Assert.False(
            await _processor.RecordReauthenticationAsync(_userId, Identity(), Guid.CreateVersion7())
        );
    }

    [Fact]
    public async Task CountSoleSignInUsers_CountsUsersWithNoOtherWayIn()
    {
        var onlyGoogle = Guid.CreateVersion7();
        var withPassword = Guid.CreateVersion7();
        var withGitHub = Guid.CreateVersion7();
        await _processor.LinkAsync(onlyGoogle, Identity(subject: "a"));
        await _processor.LinkAsync(withPassword, Identity(subject: "b"));
        await GivePasswordAsync(withPassword);
        await _processor.LinkAsync(withGitHub, Identity(subject: "c"));
        await _processor.LinkAsync(withGitHub, Identity(IdentityProviderType.GitHub, "d"));

        Assert.Equal(1, await _processor.CountSoleSignInUsersAsync(IdentityProviderType.Google));
        Assert.Equal(0, await _processor.CountSoleSignInUsersAsync(IdentityProviderType.Amazon));
    }
}

using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Extensions;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Entities;
using Corely.Security.Hashing.Factories;

namespace Corely.IAM.UnitTests.Security.Extensions;

public class AuthTokenEntityExtensionsTests
{
    private static readonly DateTime _utcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly SecurityPolicy _policy = SecurityPolicy.Default with
    {
        AuthSessionTtlSeconds = 3600,
        RefreshReuseGraceSeconds = 30,
    };

    private static AuthTokenEntity Token(
        DateTime? revokedUtc = null,
        Guid? replacedByTokenId = null,
        int refreshExpiresInSeconds = 60,
        int sessionStartedSecondsAgo = 0
    ) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            SessionId = Guid.CreateVersion7(),
            SessionStartedUtc = _utcNow.AddSeconds(-sessionStartedSecondsAgo),
            RefreshExpiresUtc = _utcNow.AddSeconds(refreshExpiresInSeconds),
            RevokedUtc = revokedUtc,
            ReplacedByTokenId = replacedByTokenId,
        };

    [Fact]
    public void RenewalStateAt_IsRenewable_ForALiveToken() =>
        Assert.Equal(AuthTokenRenewalState.Renewable, Token().RenewalStateAt(_utcNow, _policy));

    [Theory]
    [InlineData(29, AuthTokenRenewalState.RecentlyReplaced)]
    [InlineData(30, AuthTokenRenewalState.Reused)]
    [InlineData(120, AuthTokenRenewalState.Reused)]
    public void RenewalStateAt_SeparatesAReplayInsideTheGrace_FromALateReuse(
        int replacedSecondsAgo,
        Enum expected
    ) =>
        Assert.Equal(
            (AuthTokenRenewalState)expected,
            Token(_utcNow.AddSeconds(-replacedSecondsAgo), Guid.CreateVersion7())
                .RenewalStateAt(_utcNow, _policy)
        );

    [Fact]
    public void RenewalStateAt_IsRevoked_ForARevokedTokenThatWasNotReplaced() =>
        Assert.Equal(
            AuthTokenRenewalState.Revoked,
            Token(revokedUtc: _utcNow.AddSeconds(-1)).RenewalStateAt(_utcNow, _policy)
        );

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(60, 3600)]
    public void RenewalStateAt_IsExpired_AtTheRefreshExpiryOrTheSessionCap(
        int refreshExpiresInSeconds,
        int sessionStartedSecondsAgo
    ) =>
        Assert.Equal(
            AuthTokenRenewalState.Expired,
            Token(
                    refreshExpiresInSeconds: refreshExpiresInSeconds,
                    sessionStartedSecondsAgo: sessionStartedSecondsAgo
                )
                .RenewalStateAt(_utcNow, _policy)
        );

    [Fact]
    public void RenewalStateAt_ReportsReplacementBeforeExpiry() =>
        Assert.Equal(
            AuthTokenRenewalState.Reused,
            Token(_utcNow.AddSeconds(-60), Guid.CreateVersion7(), refreshExpiresInSeconds: -1)
                .RenewalStateAt(_utcNow, _policy)
        );

    [Fact]
    public void HasRefreshSecret_MatchesOnlyTheHashedSecret_AndNeverAnEmptyHash()
    {
        var serviceFactory = new ServiceFactory();
        var hashProviderFactory = serviceFactory.GetRequiredService<IHashProviderFactory>();
        var hashCodes = serviceFactory.GetRequiredService<IamHashCodes>();
        var token = Token();
        token.RefreshSecretHash = hashProviderFactory
            .GetProvider(hashCodes.TokenHashCode)
            .Hash("secret");

        Assert.True(token.HasRefreshSecret("secret", hashProviderFactory));
        Assert.False(token.HasRefreshSecret("other", hashProviderFactory));
        Assert.False(
            new AuthTokenEntity { RefreshSecretHash = "" }.HasRefreshSecret(
                "secret",
                hashProviderFactory
            )
        );
    }
}

using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Authentication;

public class SessionLifecycleTests : IAsyncLifetime
{
    private const string NewPassword = "Changed1234";

    private readonly IamScenario _scenario = new();

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task ChangingThePassword_EndsOtherSessions_AndKeepsTheCurrentOne()
    {
        var other = await SignInAsync("device-other");
        var current = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(
                    _scenario.DirectMemberUsername,
                    IamScenario.Password,
                    "device-current"
                )
            );
            var change = await services
                .GetRequiredService<IModificationService>()
                .ModifyPasswordAsync(
                    new ModifyPasswordRequest(IamScenario.Password, NewPassword, null)
                );
            Assert.Equal(ModifyPasswordResultCode.Success, change.ResultCode);
            return signIn;
        });

        Assert.Equal(
            RenewAuthTokenResultCode.InvalidAuthTokenError,
            (await RenewAsync(other.RefreshToken!)).ResultCode
        );
        Assert.Equal(
            RenewAuthTokenResultCode.Success,
            (await RenewAsync(current.RefreshToken!)).ResultCode
        );
    }

    [Fact]
    public async Task ARenewedSession_KeepsOneActiveRowAndChainsTheSpentOnes()
    {
        var signIn = await SignInAsync("device-a");
        _scenario.Host.TimeProvider.AdvanceSeconds(60);
        var first = await RenewAsync(signIn.RefreshToken!);
        _scenario.Host.TimeProvider.AdvanceSeconds(60);
        var second = await RenewAsync(first.RefreshToken!);

        var rows = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .Where(t => t.UserId == _scenario.DirectMemberUserId && t.DeviceId == "device-a")
                .OrderBy(t => t.IssuedUtc)
                .ToListAsync()
        );

        Assert.Equal(3, rows.Count);
        Assert.Single(rows.Select(r => r.SessionId).Distinct());
        Assert.Single(rows, r => r.RevokedUtc == null);
        Assert.Equal(first.AuthTokenId, rows[0].ReplacedByTokenId);
        Assert.Equal(second.AuthTokenId, rows[1].ReplacedByTokenId);
        Assert.Null(rows[2].ReplacedByTokenId);
    }

    [Fact]
    public async Task ReusingASpentRefreshToken_EndsTheSessionOnRealSql()
    {
        var signIn = await SignInAsync("device-a");
        var renewed = await RenewAsync(signIn.RefreshToken!);
        _scenario.Host.TimeProvider.AdvanceSeconds(31);

        var reuse = await RenewAsync(signIn.RefreshToken!);
        var afterReuse = await RenewAsync(renewed.RefreshToken!);

        Assert.Equal(RenewAuthTokenResultCode.RefreshTokenReusedError, reuse.ResultCode);
        Assert.Equal(RenewAuthTokenResultCode.InvalidAuthTokenError, afterReuse.ResultCode);
        Assert.Equal(0, await ActiveRowCountAsync("device-a"));
    }

    [Fact]
    public async Task SwitchingAccount_ContinuesTheSession_AndSpendsTheOldRefreshToken()
    {
        var switched = await _scenario.Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(_scenario.DirectMemberUsername, IamScenario.Password, "device-a")
            );
            var result = await authentication.SwitchAccountAsync(
                new SwitchAccountRequest(_scenario.AccountId)
            );
            return (signIn, result);
        });
        _scenario.Host.TimeProvider.AdvanceSeconds(31);

        var oldRefresh = await RenewAsync(switched.signIn.RefreshToken!);

        Assert.Equal(SignInResultCode.Success, switched.result.ResultCode);
        Assert.Equal(RenewAuthTokenResultCode.RefreshTokenReusedError, oldRefresh.ResultCode);
        var sessions = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .Where(t => t.UserId == _scenario.DirectMemberUserId && t.DeviceId == "device-a")
                .Select(t => t.SessionId)
                .Distinct()
                .CountAsync()
        );
        Assert.Equal(1, sessions);
    }

    [Fact]
    public async Task DeletingExpiredTokens_RemovesOnlyRowsPastTheirRefreshExpiry()
    {
        await SignInAsync("device-old");
        _scenario.Host.TimeProvider.AdvanceSeconds(80000);
        var live = await SignInAsync("device-new");
        _scenario.Host.TimeProvider.AdvanceSeconds(10000);

        var result = await _scenario.AsSystemAsync(services =>
            services.GetRequiredService<IAuthenticationService>().DeleteExpiredAuthTokensAsync()
        );

        Assert.Equal(DeleteExpiredAuthTokensResultCode.Success, result.ResultCode);
        Assert.True(result.DeletedCount >= 1);
        var remaining = await _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .Where(t => t.UserId == _scenario.DirectMemberUserId)
                .Select(t => t.Id)
                .ToListAsync()
        );
        Assert.Equal([live.AuthTokenId!.Value], remaining);
    }

    private Task<SignInResult> SignInAsync(string deviceId) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(
                    new SignInRequest(
                        _scenario.DirectMemberUsername,
                        IamScenario.Password,
                        deviceId
                    )
                )
        );

    private Task<RenewAuthTokenResult> RenewAsync(string refreshToken) =>
        _scenario.Host.WithScopeAsync(services =>
            services
                .GetRequiredService<IAuthenticationService>()
                .RenewAuthTokenAsync(new RenewAuthTokenRequest(refreshToken))
        );

    private Task<int> ActiveRowCountAsync(string deviceId) =>
        _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .CountAsync(t =>
                    t.UserId == _scenario.DirectMemberUserId
                    && t.DeviceId == deviceId
                    && t.RevokedUtc == null
                )
        );
}

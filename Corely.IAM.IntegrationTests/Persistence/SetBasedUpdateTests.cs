using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class SetBasedUpdateTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new();

    public Task InitializeAsync() => _scenario.InitializeAsync();

    public Task DisposeAsync() => _scenario.DisposeAsync();

    [Fact]
    public async Task RevokingAllSessions_TranslatesAndAffectsOnlyThatUser()
    {
        await SignInAsync(_scenario.DirectMemberUsername, "device-a");
        await SignInAsync(_scenario.DirectMemberUsername, "device-b");
        await SignInAsync(_scenario.GroupMemberUsername, "device-c");

        Assert.True(await ActiveTokenCountAsync(_scenario.DirectMemberUserId) >= 2);
        var otherUserBefore = await ActiveTokenCountAsync(_scenario.GroupMemberUserId);

        await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            async services =>
            {
                await services.GetRequiredService<IAuthenticationService>().SignOutAllAsync();
                return true;
            }
        );

        Assert.Equal(0, await ActiveTokenCountAsync(_scenario.DirectMemberUserId));
        Assert.Equal(otherUserBefore, await ActiveTokenCountAsync(_scenario.GroupMemberUserId));
    }

    [Fact]
    public async Task RevokingOtherSessions_LeavesTheCurrentOneActive()
    {
        await SignInAsync(_scenario.DirectMemberUsername, "device-a");
        await SignInAsync(_scenario.DirectMemberUsername, "device-b");

        var result = await _scenario.ActAsAsync(
            _scenario.DirectMemberUsername,
            _scenario.AccountId,
            services =>
                services.GetRequiredService<IAuthenticationService>().RevokeOtherSessionsAsync()
        );

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
        Assert.True(
            await ActiveTokenCountAsync(_scenario.DirectMemberUserId) >= 1,
            "The acting session must survive revoking the others."
        );
    }

    [Fact]
    public async Task RevocationWithNoMatchingRows_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
            _scenario.ActAsAsync(
                _scenario.OutsiderUsername,
                null,
                services =>
                    services.GetRequiredService<IAuthenticationService>().RevokeOtherSessionsAsync()
            )
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task RequestingASecondRecovery_InvalidatesTheFirstThroughASetBasedUpdate()
    {
        await RequestRecoveryAsync();
        await RequestRecoveryAsync();

        var pending = await _scenario.Host.QueryAsync(db =>
            db.PasswordRecoveries.AsNoTracking()
                .CountAsync(r =>
                    r.UserId == _scenario.DirectMemberUserId
                    && r.CompletedUtc == null
                    && r.InvalidatedUtc == null
                )
        );

        Assert.Equal(1, pending);
    }

    [Fact]
    public async Task InvalidationDoesNotTouchAnotherUsersRecoveries()
    {
        await RequestRecoveryAsync();
        await RequestRecoveryForAsync(_scenario.GroupMemberUsername);
        await RequestRecoveryAsync();

        var otherUserPending = await _scenario.Host.QueryAsync(db =>
            db.PasswordRecoveries.AsNoTracking()
                .CountAsync(r =>
                    r.UserId == _scenario.GroupMemberUserId && r.InvalidatedUtc == null
                )
        );

        Assert.Equal(1, otherUserPending);
    }

    private Task RequestRecoveryAsync() => RequestRecoveryForAsync(_scenario.DirectMemberUsername);

    private Task RequestRecoveryForAsync(string username) =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IPasswordRecoveryService>()
                .RequestPasswordRecoveryAsync(
                    new RequestPasswordRecoveryRequest($"{username}@example.com")
                );
            Assert.Equal(RequestPasswordRecoveryResultCode.Success, result.ResultCode);
        });

    private Task SignInAsync(string username, string deviceId) =>
        _scenario.Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest(username, IamScenario.Password, deviceId));
            Assert.Equal(SignInResultCode.Success, result.ResultCode);
        });

    private Task<int> ActiveTokenCountAsync(Guid userId)
    {
        var now = _scenario.Host.TimeProvider.GetUtcNow().UtcDateTime;
        return _scenario.Host.QueryAsync(db =>
            db.UserAuthTokens.AsNoTracking()
                .CountAsync(t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now)
        );
    }
}

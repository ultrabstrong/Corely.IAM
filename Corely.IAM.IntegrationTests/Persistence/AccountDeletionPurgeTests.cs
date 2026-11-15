using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Persistence;

public class AccountDeletionPurgeTests : IAsyncLifetime
{
    private readonly IamScenario _scenario = new() { PurgesAccountDataOnDeletion = true };

    public ValueTask InitializeAsync() => _scenario.InitializeAsync();

    public ValueTask DisposeAsync() => _scenario.DisposeAsync();

    private Task<DeregisterAccountAccessResult> RemoveAccessAsOwnerAsync() =>
        _scenario.ActAsAsync(
            _scenario.OwnerUsername,
            _scenario.AccountId,
            services =>
                services
                    .GetRequiredService<IDeregistrationService>()
                    .DeregisterAccountAccessAsync(
                        new DeregisterAccountAccessRequest(_scenario.AccountId)
                    )
        );

    private Task<DeregisterAccountResult> DeleteAsSystemAsync() =>
        _scenario.AsSystemAsync(services =>
            services
                .GetRequiredService<IDeregistrationService>()
                .DeregisterAccountAsync(new DeregisterAccountRequest(_scenario.AccountId))
        );

    [Fact]
    public async Task DeregisterAccountAccess_RemovesEveryMemberIncludingTheSoleOwner_ForTheOwnersAccount()
    {
        var result = await RemoveAccessAsOwnerAsync();

        Assert.Equal(DeregisterAccountAccessResultCode.Success, result.ResultCode);
        var members = await _scenario.Host.QueryAsync(db =>
            db.Accounts.AsNoTracking()
                .Where(a => a.Id == _scenario.AccountId)
                .SelectMany(a => a.Users!)
                .CountAsync()
        );
        Assert.Equal(0, members);
    }

    [Fact]
    public async Task DeregisterAccountAccess_LeavesTheOtherAccountsMembers_ForTheOwnersAccount()
    {
        var before = await MembersOfAsync(_scenario.OtherAccountId);

        await RemoveAccessAsOwnerAsync();

        Assert.Equal(before, await MembersOfAsync(_scenario.OtherAccountId));
    }

    [Fact]
    public async Task DeregisterAccount_LeavesOnlyANamelessDeletionEntry_ForAHostThatPurgesAccountData()
    {
        await RemoveAccessAsOwnerAsync();

        var result = await DeleteAsSystemAsync();

        Assert.Equal(DeregisterAccountResultCode.Success, result.ResultCode);
        var entries = await _scenario.Host.QueryAsync(db =>
            db.AuditEntries.AsNoTracking()
                .Where(e => e.AccountId == _scenario.AccountId)
                .ToListAsync()
        );
        var entry = Assert.Single(entries);
        Assert.Equal(nameof(IDeregistrationService.DeregisterAccountAsync), entry.Operation);
        Assert.Null(entry.Details);
    }

    [Fact]
    public async Task DeregisterAccount_DeletesTheAccountsTokensAndChallenges_ForAHostThatPurgesAccountData()
    {
        await RemoveAccessAsOwnerAsync();
        var tokensBefore = await _scenario.Host.QueryAsync(db =>
            db.AuthTokens.CountAsync(t => t.AccountId == _scenario.AccountId)
        );
        Assert.True(tokensBefore > 0);

        await DeleteAsSystemAsync();

        var left = await _scenario.Host.QueryAsync(async db =>
            await db.AuthTokens.CountAsync(t => t.AccountId == _scenario.AccountId)
            + await db.MfaChallenges.CountAsync(c => c.AccountId == _scenario.AccountId)
        );
        Assert.Equal(0, left);
    }

    [Fact]
    public async Task DeregisterAccount_SucceedsAsNotFound_ForASecondRun()
    {
        await RemoveAccessAsOwnerAsync();
        await DeleteAsSystemAsync();

        var again = await DeleteAsSystemAsync();

        Assert.Equal(DeregisterAccountResultCode.AccountNotFoundError, again.ResultCode);
    }

    private Task<int> MembersOfAsync(Guid accountId) =>
        _scenario.Host.QueryAsync(db =>
            db.Accounts.AsNoTracking()
                .Where(a => a.Id == accountId)
                .SelectMany(a => a.Users!)
                .CountAsync()
        );
}

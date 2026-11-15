using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Invitations.Entities;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Security.Entities;
using Corely.IAM.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountClosureProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();

    private IRepo<T> Repo<T>()
        where T : class => _serviceFactory.GetRequiredService<IRepo<T>>();

    private AccountClosureProcessor Processor(bool purges) =>
        new(
            Repo<AccountEntity>(),
            Repo<ApiKeyEntity>(),
            Repo<InvitationEntity>(),
            Repo<AuditEntryEntity>(),
            Repo<AuthTokenEntity>(),
            Repo<MfaChallengeEntity>(),
            new AccountDeletionPolicy(purges),
            TimeProvider.System,
            NullLogger<AccountClosureProcessor>.Instance
        );

    private async Task<Guid> CreateAccountAsync(int members, bool platform = false)
    {
        var users = new List<UserEntity>();
        for (var i = 0; i < members; i++)
        {
            users.Add(
                await Repo<UserEntity>()
                    .CreateAsync(
                        new UserEntity
                        {
                            Id = Guid.CreateVersion7(),
                            Accounts = [],
                            Groups = [],
                            Roles = [],
                        }
                    )
            );
        }

        var account = await Repo<AccountEntity>()
            .CreateAsync(
                new AccountEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountName = $"account-{Guid.CreateVersion7()}",
                    IsPlatformAccount = platform,
                    Users = users,
                }
            );
        return account.Id;
    }

    private Task<ApiKeyEntity> CreateApiKeyAsync(Guid accountId) =>
        Repo<ApiKeyEntity>()
            .CreateAsync(
                new ApiKeyEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    Name = $"key-{Guid.CreateVersion7()}",
                    Roles = [],
                    Groups = [],
                }
            );

    private Task<InvitationEntity> CreateInvitationAsync(Guid accountId, Guid? acceptedBy = null) =>
        Repo<InvitationEntity>()
            .CreateAsync(
                new InvitationEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    Email = "invitee@example.com",
                    SecretHash = "hash",
                    AcceptedByUserId = acceptedBy,
                }
            );

    private async Task SeedSignInDataAsync(Guid accountId)
    {
        await Repo<AuditEntryEntity>()
            .CreateAsync(
                new AuditEntryEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    Source = "test",
                    Service = "service",
                    Operation = "operation",
                    ResourceType = "account",
                    ResultCode = "Success",
                }
            );
        await Repo<AuthTokenEntity>()
            .CreateAsync(
                new AuthTokenEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    DeviceId = "device",
                    RefreshSecretHash = "hash",
                }
            );
        await Repo<MfaChallengeEntity>()
            .CreateAsync(
                new MfaChallengeEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = accountId,
                    ChallengeToken = $"token-{Guid.CreateVersion7()}",
                    DeviceId = "device",
                }
            );
    }

    private async Task<int> SignInRowsForAsync(Guid accountId) =>
        (await Repo<AuditEntryEntity>().ListAsync(e => e.AccountId == accountId)).Count
        + (await Repo<AuthTokenEntity>().ListAsync(t => t.AccountId == accountId)).Count
        + (await Repo<MfaChallengeEntity>().ListAsync(c => c.AccountId == accountId)).Count;

    [Fact]
    public async Task RemoveAccountAccessAsync_RemovesEveryMemberIncludingTheSoleOwner_ForAnAccount()
    {
        var accountId = await CreateAccountAsync(members: 2);

        var result = await Processor(purges: false).RemoveAccountAccessAsync(accountId);

        Assert.Equal(CloseAccountResultCode.Success, result.ResultCode);
        var account = await Repo<AccountEntity>()
            .GetAsync(a => a.Id == accountId, include: q => q.Include(a => a.Users));
        Assert.Empty(account!.Users!);
    }

    [Fact]
    public async Task RemoveAccountAccessAsync_DeletesApiKeysAndRevokesPendingInvitations_ForAnAccount()
    {
        var accountId = await CreateAccountAsync(members: 1);
        var otherId = await CreateAccountAsync(members: 1);
        await CreateApiKeyAsync(accountId);
        var otherKey = await CreateApiKeyAsync(otherId);
        var pending = await CreateInvitationAsync(accountId);
        var accepted = await CreateInvitationAsync(accountId, acceptedBy: Guid.CreateVersion7());

        await Processor(purges: false).RemoveAccountAccessAsync(accountId);

        Assert.Empty(await Repo<ApiKeyEntity>().ListAsync(k => k.AccountId == accountId));
        Assert.NotNull(await Repo<ApiKeyEntity>().GetAsync(k => k.Id == otherKey.Id));
        Assert.NotNull(
            (await Repo<InvitationEntity>().GetAsync(i => i.Id == pending.Id))!.RevokedUtc
        );
        Assert.Null(
            (await Repo<InvitationEntity>().GetAsync(i => i.Id == accepted.Id))!.RevokedUtc
        );
    }

    [Fact]
    public async Task RemoveAccountAccessAsync_RefusesAndKeepsMembers_ForThePlatformAccount()
    {
        var accountId = await CreateAccountAsync(members: 1, platform: true);

        var result = await Processor(purges: false).RemoveAccountAccessAsync(accountId);

        Assert.Equal(CloseAccountResultCode.PlatformAccountError, result.ResultCode);
        var account = await Repo<AccountEntity>()
            .GetAsync(a => a.Id == accountId, include: q => q.Include(a => a.Users));
        Assert.Single(account!.Users!);
    }

    [Fact]
    public async Task RemoveAccountAccessAsync_ReturnsNotFound_ForAnAccountThatDoesNotExist()
    {
        var result = await Processor(purges: false).RemoveAccountAccessAsync(Guid.CreateVersion7());

        Assert.Equal(CloseAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task PurgeAccountDataAsync_DeletesAuditEntriesTokensAndChallenges_ForAHostThatOptedIn()
    {
        var accountId = await CreateAccountAsync(members: 1);
        var otherId = await CreateAccountAsync(members: 1);
        await SeedSignInDataAsync(accountId);
        await SeedSignInDataAsync(otherId);

        var result = await Processor(purges: true).PurgeAccountDataAsync(accountId);

        Assert.Equal(CloseAccountResultCode.Success, result.ResultCode);
        Assert.Equal(0, await SignInRowsForAsync(accountId));
        Assert.Equal(3, await SignInRowsForAsync(otherId));
    }

    [Fact]
    public async Task PurgeAccountDataAsync_KeepsEverything_ForAHostThatDidNotOptIn()
    {
        var accountId = await CreateAccountAsync(members: 1);
        await SeedSignInDataAsync(accountId);

        var result = await Processor(purges: false).PurgeAccountDataAsync(accountId);

        Assert.Equal(CloseAccountResultCode.Success, result.ResultCode);
        Assert.Equal(3, await SignInRowsForAsync(accountId));
    }

    [Fact]
    public async Task PurgeAccountDataAsync_RefusesAndKeepsEverything_ForThePlatformAccount()
    {
        var accountId = await CreateAccountAsync(members: 1, platform: true);
        await SeedSignInDataAsync(accountId);

        var result = await Processor(purges: true).PurgeAccountDataAsync(accountId);

        Assert.Equal(CloseAccountResultCode.PlatformAccountError, result.ResultCode);
        Assert.Equal(3, await SignInRowsForAsync(accountId));
    }
}

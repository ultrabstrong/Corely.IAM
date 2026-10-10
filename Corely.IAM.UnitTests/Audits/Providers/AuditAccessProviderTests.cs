using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Audits.Providers;

public class AuditAccessProviderTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IUserContextProvider> _userContextProvider = new();
    private readonly AuditAccessProvider _provider;
    private readonly Guid _userId = Guid.CreateVersion7();

    public AuditAccessProviderTests()
    {
        _provider = new AuditAccessProvider(
            _userContextProvider.Object,
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>()
        );
    }

    [Fact]
    public async Task GetAccess_ReachesNothing_WithoutAContext()
    {
        var access = await _provider.GetAccessAsync(AuthAction.Read);

        Assert.False(access.Everything);
        Assert.Empty(access.AccountIds);
        Assert.Null(access.ViewerUserId);
    }

    [Fact]
    public async Task GetAccess_ReachesEverything_UnderSystemContext()
    {
        _userContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new UserContext(true, "system"));

        Assert.True((await _provider.GetAccessAsync(AuthAction.Delete)).Everything);
    }

    [Fact]
    public async Task GetAccess_ReachesTheAccountsWhereTheUserHoldsTheAction()
    {
        SignIn();
        var readable = await GrantAsync(isPlatform: false, read: true);
        await GrantAsync(isPlatform: false, delete: true);

        var access = await _provider.GetAccessAsync(AuthAction.Read);

        Assert.False(access.Everything);
        Assert.Equal([readable], access.AccountIds);
        Assert.Equal(_userId, access.ViewerUserId);
    }

    [Fact]
    public async Task GetAccess_CountsAGrantThroughAGroup()
    {
        SignIn();
        var accountId = await GrantAsync(isPlatform: false, read: true, throughGroup: true);

        Assert.Contains(accountId, (await _provider.GetAccessAsync(AuthAction.Read)).AccountIds);
    }

    [Fact]
    public async Task GetAccess_ReachesEverything_ForAWildcardInThePlatformAccount()
    {
        SignIn();
        await GrantAsync(isPlatform: true, read: true);

        Assert.True((await _provider.GetAccessAsync(AuthAction.Read)).Everything);
    }

    [Fact]
    public async Task HoldsInPlatformAccount_IgnoresTheSameGrantInACustomerAccount()
    {
        SignIn();
        await GrantAsync(
            isPlatform: false,
            update: true,
            resourceType: AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
        );

        Assert.False(
            await _provider.HoldsInPlatformAccountAsync(
                AuthAction.Update,
                AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task HoldsInPlatformAccount_ReturnsTrue_ForAGrantInThePlatformAccount()
    {
        SignIn();
        await GrantAsync(
            isPlatform: true,
            update: true,
            resourceType: AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
        );

        Assert.True(
            await _provider.HoldsInPlatformAccountAsync(
                AuthAction.Update,
                AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
            )
        );
        Assert.False(
            await _provider.HoldsInPlatformAccountAsync(
                AuthAction.Read,
                AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
            )
        );
    }

    private void SignIn() =>
        _userContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(
                new UserContext(
                    new User { Id = _userId, Username = "viewer" },
                    new Account { Id = Guid.CreateVersion7(), AccountName = "current" },
                    "device",
                    []
                )
            );

    private async Task<Guid> GrantAsync(
        bool isPlatform,
        bool read = false,
        bool update = false,
        bool delete = false,
        bool throughGroup = false,
        string resourceType = AuditConstants.AUDIT_RESOURCE_TYPE
    )
    {
        var account = new AccountEntity
        {
            Id = Guid.CreateVersion7(),
            AccountName = $"account-{Guid.CreateVersion7()}",
            IsPlatformAccount = isPlatform,
        };
        await _serviceFactory.GetRequiredService<IRepo<AccountEntity>>().CreateAsync(account);

        var user = new UserEntity { Id = _userId, Username = "viewer" };
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "Auditors",
            AccountId = account.Id,
            Account = account,
            Users = throughGroup ? [] : [user],
            Groups = throughGroup
                ?
                [
                    new GroupEntity
                    {
                        Id = Guid.CreateVersion7(),
                        Name = "Group",
                        AccountId = account.Id,
                        Users = [user],
                    },
                ]
                : [],
        };
        await _serviceFactory
            .GetRequiredService<IRepo<PermissionEntity>>()
            .CreateAsync(
                new PermissionEntity
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    Account = account,
                    ResourceType = resourceType,
                    ResourceId = Guid.Empty,
                    Read = read,
                    Update = update,
                    Delete = delete,
                    Roles = [role],
                }
            );
        return account.Id;
    }
}

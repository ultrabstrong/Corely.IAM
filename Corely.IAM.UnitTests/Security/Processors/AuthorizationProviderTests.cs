using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AuthorizationProviderTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly ControllableTimeProvider _timeProvider = new();
    private const int CACHE_TTL_SECONDS = 30;

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenUserHasPermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenUserHasPermissionForSpecificResource()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        var resourceId = Guid.CreateVersion7();
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: resourceId,
            update: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenUserHasWildcardPermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.CreateVersion7()
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenNoPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenWrongAction()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenWrongResourceType()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ROLE_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenWrongResourceId()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.CreateVersion7(),
            update: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.CreateVersion7()
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenUserHasWildcardResourceTypePermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ALL_RESOURCE_TYPES,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.ROLE_RESOURCE_TYPE
            )
        );
        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.USER_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task IsAuthorized_CachesPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true,
            read: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Read,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public void IsAuthorizedForOwnUser_ReturnsTrue_WhenUserMatchesContext()
    {
        var provider = CreateProvider();
        var userId = Guid.CreateVersion7();
        SetUserContext(userId, Guid.CreateVersion7());

        var result = provider.IsAuthorizedForOwnUser(userId);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForOwnUser_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();

        var result = provider.IsAuthorizedForOwnUser(Guid.CreateVersion7());

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorizedForOwnUser_ReturnsFalse_WhenUserDoesNotMatchContext()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = provider.IsAuthorizedForOwnUser(Guid.CreateVersion7());

        Assert.False(result);
    }

    [Fact]
    public void HasUserContext_ReturnsTrue_WhenUserContextExists()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = provider.HasUserContext();

        Assert.True(result);
    }

    [Fact]
    public void HasUserContext_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();

        var result = provider.HasUserContext();

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsTrue_WhenUserHasAccessToCurrentAccount()
    {
        var provider = CreateProvider();
        var account = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: account,
            availableAccounts: [account]
        );

        var result = provider.HasAccountContext(account.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();

        var result = provider.HasAccountContext(Guid.CreateVersion7());

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenCurrentAccountIsNull()
    {
        var provider = CreateProvider();
        var account = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: null,
            availableAccounts: [account]
        );

        var result = provider.HasAccountContext(account.Id);

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenCurrentAccountNotInAvailableAccounts()
    {
        var provider = CreateProvider();
        var currentAccount = new Account() { Id = Guid.CreateVersion7() };
        var availableAccount = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: currentAccount,
            availableAccounts: [availableAccount]
        );

        var result = provider.HasAccountContext(currentAccount.Id);

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsTrue_WhenCurrentAccountIsOneOfMultipleAvailableAccounts()
    {
        var provider = CreateProvider();
        var account1 = new Account() { Id = Guid.CreateVersion7() };
        var account2 = new Account() { Id = Guid.CreateVersion7() };
        var account3 = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: account2,
            availableAccounts: [account1, account2, account3]
        );

        var result = provider.HasAccountContext(account2.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenAvailableAccountsIsEmpty()
    {
        var provider = CreateProvider();
        var currentAccount = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: currentAccount,
            availableAccounts: []
        );

        var result = provider.HasAccountContext(currentAccount.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsFalse_WhenUserHasPermissionForOnlyOneOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        var resourceIdWithPermission = Guid.CreateVersion7();
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: resourceIdWithPermission,
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            resourceIdWithPermission,
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );

        Assert.False(result);
    }

    [Fact]
    public async Task ClearCache_ForcesPermissionReload_OnNextAuthorization()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );

        ((IAuthorizationCacheClearer)provider).ClearCache();
        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenPermissionGrantedViaGroupMembership()
    {
        var provider = CreateProvider();
        var userId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        SetUserContext(userId, accountId);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var account = new AccountEntity { Id = accountId, AccountName = "TestAccount" };
        await accountRepo.CreateAsync(account);

        var user = new UserEntity
        {
            Id = userId,
            Username = "testuser",
            Email = "test@test.com",
        };
        await userRepo.CreateAsync(user);

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "TestGroup",
            AccountId = accountId,
            Users = [user],
        };
        await groupRepo.CreateAsync(group);

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "TestRole",
            AccountId = accountId,
            Groups = [group],
            Users = [],
        };
        await roleRepo.CreateAsync(role);

        var permission = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            ResourceType = PermissionConstants.ROLE_RESOURCE_TYPE,
            ResourceId = Guid.Empty,
            Read = true,
            Roles = [role],
        };
        await permissionRepo.CreateAsync(permission);

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorized_InvalidatesCache_WhenAccountContextChanges()
    {
        var provider = CreateProvider();
        var userId = Guid.CreateVersion7();
        var account1 = new Account() { Id = Guid.CreateVersion7() };
        var account2 = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(userId, account1, [account1, account2]);

        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );

        SetUserContext(userId, account2, [account1, account2]);

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorized_ReturnsTrue_WhenUserHasPermissionForAllOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        var resourceId1 = Guid.CreateVersion7();
        var resourceId2 = Guid.CreateVersion7();
        var resourceId3 = Guid.CreateVersion7();
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceIds: new[] { resourceId1, resourceId2, resourceId3 },
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            resourceId1,
            resourceId2,
            resourceId3
        );

        Assert.True(result);
    }

    private AuthorizationProvider CreateProvider()
    {
        return new AuthorizationProvider(
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>(),
            Options.Create(new SecurityOptions { PermissionCacheTtlSeconds = CACHE_TTL_SECONDS }),
            _timeProvider
        );
    }

    private void SetUserContext(Guid userId, Guid? accountId)
    {
        var account = accountId == null ? null : new Account() { Id = accountId.Value };
        SetUserContext(userId, account, account == null ? [] : [account]);
    }

    private void SetUserContext(
        Guid userId,
        Account? currentAccount,
        List<Account> availableAccounts
    )
    {
        var userContextProvider = _serviceFactory.GetRequiredService<UserContextProvider>();
        ((IUserContextSetter)userContextProvider).SetUserContext(
            new UserContext(new User() { Id = userId }, currentAccount, null, availableAccounts)
        );
    }

    private async Task SetupTestPermissionDataAsync(
        string resourceType,
        Guid resourceId,
        bool create = false,
        bool read = false,
        bool update = false,
        bool delete = false,
        bool execute = false
    )
    {
        await SetupTestPermissionDataAsync(
            resourceType,
            [resourceId],
            create,
            read,
            update,
            delete,
            execute
        );
    }

    private async Task SetupTestPermissionDataAsync(
        string resourceType,
        Guid[] resourceIds,
        bool create = false,
        bool read = false,
        bool update = false,
        bool delete = false,
        bool execute = false
    )
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var userContextProvider = _serviceFactory.GetRequiredService<UserContextProvider>();
        var context = userContextProvider.GetUserContext();

        var account = new AccountEntity
        {
            Id = context!.CurrentAccount?.Id ?? Guid.CreateVersion7(),
            AccountName = "TestAccount",
        };
        await accountRepo.CreateAsync(account);

        var user = new UserEntity
        {
            Id = context.User!.Id,
            Username = "testuser",
            Email = "test@test.com",
        };
        await userRepo.CreateAsync(user);

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "TestRole",
            AccountId = account.Id,
            Users = [user],
            Groups = [],
        };
        await roleRepo.CreateAsync(role);

        foreach (var resourceId in resourceIds)
        {
            var permission = new PermissionEntity
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Create = create,
                Read = read,
                Update = update,
                Delete = delete,
                Execute = execute,
                Roles = [role],
            };
            await permissionRepo.CreateAsync(permission);
        }
    }

    // Deletes rather than clearing flags: MockRepo shares instances with the cache.
    private async Task RevokeAllPermissionsAsync()
    {
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        foreach (var permission in await permissionRepo.ListAsync(_ => true))
        {
            await permissionRepo.DeleteAsync(permission);
        }
    }

    [Fact]
    public async Task Permissions_AreCached_WithinTheTtl()
    {
        var provider = CreateProvider();
        var userId = Guid.CreateVersion7();
        SetUserContext(userId, Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );

        await RevokeAllPermissionsAsync();
        _timeProvider.Advance(TimeSpan.FromSeconds(CACHE_TTL_SECONDS - 1));

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task Permissions_AreReloaded_AfterTheTtlExpires()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );

        await RevokeAllPermissionsAsync();
        _timeProvider.Advance(TimeSpan.FromSeconds(CACHE_TTL_SECONDS));

        Assert.False(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task CacheTtl_IsAbsolute_NotSliding()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );
        await RevokeAllPermissionsAsync();

        for (var i = 0; i < CACHE_TTL_SECONDS; i++)
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(1));
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            );
        }

        Assert.False(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    [Fact]
    public async Task ClearCache_ForcesAReload_WithoutWaitingOutTheTtl()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
            create: true
        );

        Assert.True(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );

        await RevokeAllPermissionsAsync();
        ((IAuthorizationCacheClearer)provider).ClearCache();

        Assert.False(
            await provider.IsAuthorizedAsync(
                AuthAction.Create,
                PermissionConstants.GROUP_RESOURCE_TYPE
            )
        );
    }

    private sealed class ControllableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
    }
}

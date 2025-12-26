using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AuthorizationProviderTests
{
    private readonly ServiceFactory _serviceFactory = new();

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasPermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            create: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasPermissionForSpecificResource()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 5,
            update: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            5
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardPermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0, // Wildcard - applies to all
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            99
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();
        // Don't set user context

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenNoPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        // Don't setup any permissions

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenWrongAction()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            read: true // Only has Read
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenWrongResourceType()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ROLE_RESOURCE_TYPE, // Has role permission
            resourceId: 0,
            create: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenWrongResourceId()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 5, // Only for resource 5
            update: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            99
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardResourceTypePermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ALL_RESOURCE_TYPES, // Wildcard - applies to all resource types
            resourceId: 0,
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
    public async Task IsAuthorizedAsync_CachesPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
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
        SetUserContext(5, 1);

        var result = provider.IsAuthorizedForOwnUser(5);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForOwnUser_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();
        // Don't set user context

        var result = provider.IsAuthorizedForOwnUser(5);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorizedForOwnUser_ReturnsFalse_WhenUserDoesNotMatchContext()
    {
        var provider = CreateProvider();
        SetUserContext(99, 1);

        var result = provider.IsAuthorizedForOwnUser(5);

        Assert.False(result);
    }

    [Fact]
    public void HasUserContext_ReturnsTrue_WhenUserContextExists()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);

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
        var account = new Account() { Id = 1 };
        SetUserContext(userId: 1, currentAccount: account, availableAccounts: [account]);

        var result = provider.HasAccountContext();

        Assert.True(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();
        // Don't set user context

        var result = provider.HasAccountContext();

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenCurrentAccountIsNull()
    {
        var provider = CreateProvider();
        var account = new Account() { Id = 1 };
        SetUserContext(userId: 1, currentAccount: null, availableAccounts: [account]);

        var result = provider.HasAccountContext();

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenCurrentAccountNotInAvailableAccounts()
    {
        var provider = CreateProvider();
        var currentAccount = new Account() { Id = 99 };
        var availableAccount = new Account() { Id = 1 };
        SetUserContext(
            userId: 1,
            currentAccount: currentAccount,
            availableAccounts: [availableAccount]
        );

        var result = provider.HasAccountContext();

        Assert.False(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsTrue_WhenCurrentAccountIsOneOfMultipleAvailableAccounts()
    {
        var provider = CreateProvider();
        var account1 = new Account() { Id = 1 };
        var account2 = new Account() { Id = 2 };
        var account3 = new Account() { Id = 3 };
        SetUserContext(
            userId: 1,
            currentAccount: account2,
            availableAccounts: [account1, account2, account3]
        );

        var result = provider.HasAccountContext();

        Assert.True(result);
    }

    [Fact]
    public void HasAccountContext_ReturnsFalse_WhenAvailableAccountsIsEmpty()
    {
        var provider = CreateProvider();
        var currentAccount = new Account() { Id = 1 };
        SetUserContext(userId: 1, currentAccount: currentAccount, availableAccounts: []);

        var result = provider.HasAccountContext();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasPermissionForOneOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: 5, // Only has permission for resource 5
            read: true
        );

        // Request access to resources 5, 10, 15 - user only has permission for 5
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            5,
            10,
            15
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenUserHasNoPermissionForAnyOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: 5, // Only has permission for resource 5
            read: true
        );

        // Request access to resources 10, 15, 20 - user has no permission for any
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            10,
            15,
            20
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardPermissionForMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: 0, // Wildcard - applies to all resources
            read: true
        );

        // Request access to multiple resources - wildcard should cover all
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            5,
            10,
            15,
            20,
            25
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WithEmptyResourceIds_WhenUserHasGeneralPermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            create: true
        );

        // No resource IDs passed (empty array) - should check general permission
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Create,
            PermissionConstants.GROUP_RESOURCE_TYPE
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WithMultipleResourceIds_WhenWrongAction()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: 5,
            read: true // Only has Read
        );

        // Request Update action for resource 5 - user only has Read
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.USER_RESOURCE_TYPE,
            5,
            10
        );

        Assert.False(result);
    }

    private AuthorizationProvider CreateProvider()
    {
        return new AuthorizationProvider(
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>()
        );
    }

    private void SetUserContext(int userId, int? accountId)
    {
        var account = accountId == null ? null : new Account() { Id = accountId.Value };
        SetUserContext(userId, account, account == null ? [] : [account]);
    }

    private void SetUserContext(
        int userId,
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
        int resourceId,
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

        var account = new AccountEntity { Id = 1, AccountName = "TestAccount" };
        await accountRepo.CreateAsync(account);

        var user = new UserEntity
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
        };
        await userRepo.CreateAsync(user);

        var role = new RoleEntity
        {
            Id = 1,
            Name = "TestRole",
            AccountId = 1,
            Users = [user],
        };
        await roleRepo.CreateAsync(role);

        var permission = new PermissionEntity
        {
            Id = 1,
            AccountId = 1,
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

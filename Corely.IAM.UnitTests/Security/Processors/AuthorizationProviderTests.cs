using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
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
    public async Task HasAccountContextAsync_ReturnsTrue_WhenUserHasAccessToAccount()
    {
        var provider = CreateProvider();
        await SetupUserWithAccountAsync(userId: 1, accountId: 1);
        SetUserContext(1, 1);

        var result = await provider.HasAccountContextAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task HasAccountContextAsync_ReturnsFalse_WhenNoUserContext()
    {
        var provider = CreateProvider();
        // Don't set user context

        var result = await provider.HasAccountContextAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task HasAccountContextAsync_ReturnsFalse_WhenUserNotSignedIntoAccount()
    {
        var provider = CreateProvider();
        await SetupUserWithAccountAsync(userId: 1, accountId: 1);
        SetUserContext(1, null); // No account in context

        var result = await provider.HasAccountContextAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task HasAccountContextAsync_ReturnsFalse_WhenUserDoesNotHaveAccessToAccount()
    {
        var provider = CreateProvider();
        await SetupUserWithAccountAsync(userId: 1, accountId: 1);
        SetUserContext(1, 99); // User context has account 99, but user only has access to account 1

        var result = await provider.HasAccountContextAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task HasAccountContextAsync_CachesAccountIds()
    {
        var provider = CreateProvider();
        await SetupUserWithAccountAsync(userId: 1, accountId: 1);
        await SetupUserWithAccountAsync(userId: 1, accountId: 2);
        SetUserContext(1, 1);

        // First call should cache
        Assert.True(await provider.HasAccountContextAsync());

        // Change context to different account the user has access to
        SetUserContext(1, 2);

        // Should still return true because account 2 was cached
        Assert.True(await provider.HasAccountContextAsync());
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
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>()
        );
    }

    private void SetUserContext(int userId, int? accountId)
    {
        var userContextProvider = _serviceFactory.GetRequiredService<UserContextProvider>();
        ((IUserContextSetter)userContextProvider).SetUserContext(
            new UserContext(userId, accountId)
        );
    }

    private async Task SetupUserWithAccountAsync(int userId, int accountId)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<IAM.Users.Entities.UserEntity>>();

        // Get or create user first
        var existingUser = await userRepo.GetAsync(u => u.Id == userId);
        if (existingUser == null)
        {
            existingUser = new IAM.Users.Entities.UserEntity
            {
                Id = userId,
                Username = $"testuser{userId}",
                Email = $"test{userId}@test.com",
                Accounts = [],
            };
            await userRepo.CreateAsync(existingUser);
        }

        // Get or create account with user in the Users collection
        var existingAccount = await accountRepo.GetAsync(a => a.Id == accountId);
        if (existingAccount == null)
        {
            existingAccount = new AccountEntity
            {
                Id = accountId,
                AccountName = $"TestAccount{accountId}",
                Users = [existingUser],
            };
            await accountRepo.CreateAsync(existingAccount);
        }
        else
        {
            existingAccount.Users ??= [];
            if (!existingAccount.Users.Any(u => u.Id == userId))
            {
                existingAccount.Users.Add(existingUser);
                await accountRepo.UpdateAsync(existingAccount);
            }
        }

        // Also update user's Accounts collection for bidirectional relationship
        existingUser.Accounts ??= [];
        if (!existingUser.Accounts.Any(a => a.Id == accountId))
        {
            existingUser.Accounts.Add(existingAccount);
            await userRepo.UpdateAsync(existingUser);
        }
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
        var userRepo = _serviceFactory.GetRequiredService<IRepo<IAM.Users.Entities.UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<IAM.Roles.Entities.RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var account = new AccountEntity { Id = 1, AccountName = "TestAccount" };
        await accountRepo.CreateAsync(account);

        var user = new IAM.Users.Entities.UserEntity
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
        };
        await userRepo.CreateAsync(user);

        var role = new IAM.Roles.Entities.RoleEntity
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

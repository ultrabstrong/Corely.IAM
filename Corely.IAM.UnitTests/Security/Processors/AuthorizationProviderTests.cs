using Corely.DataAccess.Interfaces.Repos;
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

    private AuthorizationProvider CreateProvider()
    {
        return new AuthorizationProvider(
            _serviceFactory.GetRequiredService<IIamUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<AuthorizationProvider>>()
        );
    }

    private void SetUserContext(int userId, int accountId)
    {
        var userContextProvider = _serviceFactory.GetRequiredService<IamUserContextProvider>();
        ((IIamUserContextSetter)userContextProvider).SetUserContext(
            new IamUserContext(userId, accountId)
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
        var accountRepo = _serviceFactory.GetRequiredService<
            IRepo<IAM.Accounts.Entities.AccountEntity>
        >();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<IAM.Users.Entities.UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<IAM.Roles.Entities.RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var account = new IAM.Accounts.Entities.AccountEntity
        {
            Id = 1,
            AccountName = "TestAccount",
        };
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

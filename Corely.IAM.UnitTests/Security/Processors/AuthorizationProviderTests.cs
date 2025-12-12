using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Exceptions;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.UnitTests.Security.Processors;

public class AuthorizationProviderTests
{
    private readonly ServiceFactory _serviceFactory = new();

    [Fact]
    public async Task AuthorizeAsync_Succeeds_WhenUserHasPermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            create: true
        );

        // Should not throw
        await provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create);
    }

    [Fact]
    public async Task AuthorizeAsync_Succeeds_WhenUserHasPermissionForSpecificResource()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 5,
            update: true
        );

        await provider.AuthorizeAsync(
            PermissionConstants.GROUP_RESOURCE_TYPE,
            AuthAction.Update,
            5
        );
    }

    [Fact]
    public async Task AuthorizeAsync_Succeeds_WhenUserHasWildcardPermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0, // Wildcard - applies to all
            read: true
        );

        // Should succeed for any specific resource ID
        await provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Read, 99);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsAuthorizationException_WhenNoUserContext()
    {
        var provider = CreateProvider();
        // Don't set user context

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create)
        );

        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, exception.ResourceType);
        Assert.Equal(AuthAction.Create.ToString(), exception.RequiredAction);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsAuthorizationException_WhenNoPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        // Don't setup any permissions

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create)
        );

        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, exception.ResourceType);
        Assert.Equal(AuthAction.Create.ToString(), exception.RequiredAction);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsAuthorizationException_WhenWrongAction()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            read: true // Only has Read
        );

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create) // Needs Create
        );

        Assert.Equal(AuthAction.Create.ToString(), exception.RequiredAction);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsAuthorizationException_WhenWrongResourceType()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ROLE_RESOURCE_TYPE, // Has role permission
            resourceId: 0,
            create: true
        );

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create) // Needs group permission
        );

        Assert.Equal(PermissionConstants.GROUP_RESOURCE_TYPE, exception.ResourceType);
    }

    [Fact]
    public async Task AuthorizeAsync_ThrowsAuthorizationException_WhenWrongResourceId()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 5, // Only for resource 5
            update: true
        );

        var exception = await Assert.ThrowsAsync<AuthorizationException>(() =>
            provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Update, 99) // Needs resource 99
        );

        Assert.Equal(99, exception.ResourceId);
    }

    [Fact]
    public async Task AuthorizeAsync_Succeeds_WhenUserHasWildcardResourceTypePermission()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ALL_RESOURCE_TYPES, // Wildcard - applies to all resource types
            resourceId: 0,
            create: true
        );

        // Should succeed for any resource type
        await provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create);
        await provider.AuthorizeAsync(PermissionConstants.ROLE_RESOURCE_TYPE, AuthAction.Create);
        await provider.AuthorizeAsync(PermissionConstants.USER_RESOURCE_TYPE, AuthAction.Create);
    }

    [Fact]
    public async Task AuthorizeAsync_CachesPermissions()
    {
        var provider = CreateProvider();
        SetUserContext(1, 1);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: 0,
            create: true,
            read: true
        );

        // Multiple calls should work (using cached permissions)
        await provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Create);
        await provider.AuthorizeAsync(PermissionConstants.GROUP_RESOURCE_TYPE, AuthAction.Read);
    }

    private AuthorizationProvider CreateProvider()
    {
        return new AuthorizationProvider(
            _serviceFactory.GetRequiredService<IIamUserContextProvider>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>()
        );
    }

    private void SetUserContext(int userId, int accountId)
    {
        var userContextProvider = _serviceFactory.GetRequiredService<IIamUserContextProvider>();
        userContextProvider.SetUserContext(new IamUserContext(userId, accountId));
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

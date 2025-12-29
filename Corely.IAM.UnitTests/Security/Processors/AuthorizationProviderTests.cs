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
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasPermissionForSpecificResource()
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
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardPermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), null);
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty, // Wildcard - applies to all
            read: true
        );

        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            Guid.NewGuid()
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
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
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
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
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
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ROLE_RESOURCE_TYPE, // Has role permission
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
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenWrongResourceId()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.CreateVersion7(), // Only for resource 5
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
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardResourceTypePermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.ALL_RESOURCE_TYPES, // Wildcard - applies to all resource types
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
    public async Task IsAuthorizedAsync_CachesPermissions()
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
        // Don't set user context

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
        var account = new Account() { Id = Guid.CreateVersion7() };
        SetUserContext(
            userId: Guid.CreateVersion7(),
            currentAccount: null,
            availableAccounts: [account]
        );

        var result = provider.HasAccountContext();

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

        var result = provider.HasAccountContext();

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

        var result = provider.HasAccountContext();

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

        var result = provider.HasAccountContext();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasPermissionForOneOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        var resourceIdWithPermission = Guid.CreateVersion7();
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: resourceIdWithPermission,
            read: true
        );

        // Request access to resources 5, 10, 15 - user only has permission for 5
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            resourceIdWithPermission,
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsFalse_WhenUserHasNoPermissionForAnyOfMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: Guid.CreateVersion7(), // Only has permission for resource 5
            read: true
        );

        // Request access to resources 10, 15, 20 - user has no permission for any
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );

        Assert.False(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WhenUserHasWildcardPermissionForMultipleResourceIds()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: Guid.Empty, // Wildcard - applies to all resources
            read: true
        );

        // Request access to multiple resources - wildcard should cover all
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7()
        );

        Assert.True(result);
    }

    [Fact]
    public async Task IsAuthorizedAsync_ReturnsTrue_WithEmptyResourceIds_WhenUserHasGeneralPermission()
    {
        var provider = CreateProvider();
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.GROUP_RESOURCE_TYPE,
            resourceId: Guid.Empty,
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
        SetUserContext(Guid.CreateVersion7(), Guid.CreateVersion7());
        var resourceIdWithPermission = Guid.CreateVersion7();
        await SetupTestPermissionDataAsync(
            resourceType: PermissionConstants.USER_RESOURCE_TYPE,
            resourceId: resourceIdWithPermission,
            read: true // Only has Read
        );

        // Request Update action for resource 5 - user only has Read
        var result = await provider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.USER_RESOURCE_TYPE,
            resourceIdWithPermission,
            Guid.CreateVersion7()
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
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var permissionRepo = _serviceFactory.GetRequiredService<IRepo<PermissionEntity>>();

        var account = new AccountEntity { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };
        await accountRepo.CreateAsync(account);

        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
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
        };
        await roleRepo.CreateAsync(role);

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

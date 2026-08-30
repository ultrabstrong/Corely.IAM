using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.IntegrationTests.Infrastructure;

/// <summary>
/// The dataset the authorization matrix runs against. Mirrors what
/// <c>Corely.IAM.WebApp/DemoSetup/SeedWebAppDemo.ps1</c> builds, but as code so it runs anywhere
/// with no PATH, config file, or host state.
///
/// Shape:
/// <list type="bullet">
/// <item>An owner with two accounts, so cross-account isolation is testable.</item>
/// <item>A direct member granted a wildcard read on users via a role assigned to them.</item>
/// <item>A group member granted read+update on a single specific role, via a group-to-role
/// assignment - the longest grant path in the model.</item>
/// <item>An outsider with no membership anywhere.</item>
/// </list>
/// </summary>
public sealed class IamScenario : IAsyncLifetime
{
    public const string Password = "Scenario!Pass123";

    public IamTestHost Host { get; private set; } = null!;

    public Guid OwnerUserId { get; private set; }
    public Guid DirectMemberUserId { get; private set; }
    public Guid GroupMemberUserId { get; private set; }
    public Guid OutsiderUserId { get; private set; }

    public Guid AccountId { get; private set; }
    public Guid OtherAccountId { get; private set; }

    /// <summary>Role granted to the direct member. Carries a wildcard read on users.</summary>
    public Guid ReaderRoleId { get; private set; }

    /// <summary>Role reached only through group membership. Grants read+update on itself.</summary>
    public Guid EditorRoleId { get; private set; }

    public Guid EditorGroupId { get; private set; }

    /// <summary>A role nobody is granted anything on - the control for negative cases.</summary>
    public Guid UngrantedRoleId { get; private set; }

    public string OwnerUsername => "owner";
    public string DirectMemberUsername => "direct-member";
    public string GroupMemberUsername => "group-member";
    public string OutsiderUsername => "outsider";

    public async Task InitializeAsync()
    {
        Host = new IamTestHost();

        OwnerUserId = await RegisterUserAsync(OwnerUsername);
        DirectMemberUserId = await RegisterUserAsync(DirectMemberUsername);
        GroupMemberUserId = await RegisterUserAsync(GroupMemberUsername);
        OutsiderUserId = await RegisterUserAsync(OutsiderUsername);

        AccountId = await AsOwnerAsync(async registration =>
        {
            var account = await registration.RegisterAccountAsync(
                new RegisterAccountRequest("Primary", OwnerUserId)
            );
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            return account.CreatedAccountId;
        });

        OtherAccountId = await AsOwnerAsync(async registration =>
        {
            var account = await registration.RegisterAccountAsync(
                new RegisterAccountRequest("Secondary", OwnerUserId)
            );
            Assert.Equal(RegisterAccountResultCode.Success, account.ResultCode);
            return account.CreatedAccountId;
        });

        await AddMembersAsync();
        await BuildRolesAndPermissionsAsync();
    }

    public Task DisposeAsync()
    {
        Host.Dispose();
        return Task.CompletedTask;
    }

    // --- acting as a user --------------------------------------------------------------------

    /// <summary>
    /// Runs work under a real authenticated context for the given user, scoped to an account when
    /// one is supplied. Uses sign-in rather than a synthetic context so the permissions being
    /// tested are resolved the same way the application resolves them.
    /// </summary>
    public Task<T> ActAsAsync<T>(
        string username,
        Guid? accountId,
        Func<IServiceProvider, Task<T>> work
    ) =>
        Host.WithScopeAsync(async services =>
        {
            var authentication = services.GetRequiredService<IAuthenticationService>();
            var signIn = await authentication.SignInAsync(
                new SignInRequest(username, Password, $"device-{username}")
            );
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            if (accountId.HasValue)
            {
                var switched = await authentication.SwitchAccountAsync(
                    new SwitchAccountRequest(accountId.Value)
                );
                Assert.Equal(SignInResultCode.Success, switched.ResultCode);
            }

            return await work(services);
        });

    /// <summary>
    /// Answers the authorization question the application asks, under the given identity.
    /// </summary>
    public Task<bool> IsAuthorizedAsync(
        string username,
        Guid? accountId,
        AuthAction action,
        string resourceType,
        params Guid[] resourceIds
    ) =>
        ActAsAsync(
            username,
            accountId,
            services =>
                services
                    .GetRequiredService<IAuthorizationProvider>()
                    .IsAuthorizedAsync(action, resourceType, resourceIds)
        );

    /// <summary>Runs work under the system context, which bypasses permission checks.</summary>
    public Task<T> AsSystemAsync<T>(Func<IServiceProvider, Task<T>> work) =>
        Host.WithScopeAsync(async services =>
        {
            services.GetRequiredService<IAuthenticationService>().AuthenticateAsSystem("system");
            return await work(services);
        });

    // --- seeding ----------------------------------------------------------------------------

    private Task<Guid> RegisterUserAsync(string username) =>
        Host.WithScopeAsync(async services =>
        {
            var result = await services
                .GetRequiredService<IRegistrationService>()
                .RegisterUserAsync(
                    new RegisterUserRequest(username, $"{username}@example.com", Password)
                );
            Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);
            return result.CreatedUserId;
        });

    private Task<T> AsOwnerAsync<T>(Func<IRegistrationService, Task<T>> work) =>
        Host.WithScopeAsync(async services =>
        {
            var signIn = await services
                .GetRequiredService<IAuthenticationService>()
                .SignInAsync(new SignInRequest(OwnerUsername, Password, "device-owner"));
            Assert.Equal(SignInResultCode.Success, signIn.ResultCode);

            return await work(services.GetRequiredService<IRegistrationService>());
        });

    private Task<T> AsOwnerInAccountAsync<T>(
        Guid accountId,
        Func<IRegistrationService, Task<T>> work
    ) =>
        ActAsAsync(
            OwnerUsername,
            accountId,
            services => work(services.GetRequiredService<IRegistrationService>())
        );

    private async Task AddMembersAsync()
    {
        foreach (var userId in new[] { DirectMemberUserId, GroupMemberUserId })
        {
            var result = await AsOwnerInAccountAsync(
                AccountId,
                registration =>
                    registration.RegisterUserWithAccountAsync(
                        new RegisterUserWithAccountRequest(userId, AccountId)
                    )
            );
            Assert.Equal(RegisterUserWithAccountResultCode.Success, result.ResultCode);
        }
    }

    private async Task BuildRolesAndPermissionsAsync()
    {
        ReaderRoleId = await CreateRoleAsync("Readers", AccountId);
        EditorRoleId = await CreateRoleAsync("Editors", AccountId);
        UngrantedRoleId = await CreateRoleAsync("Ungranted", AccountId);

        // Wildcard read across every user in the account.
        var wildcardUserRead = await CreatePermissionAsync(
            AccountId,
            Permissions.Constants.PermissionConstants.USER_RESOURCE_TYPE,
            Guid.Empty,
            read: true
        );
        await AssignPermissionToRoleAsync(wildcardUserRead, ReaderRoleId, AccountId);
        await AssignRoleToUserAsync(ReaderRoleId, DirectMemberUserId, AccountId);

        // Read and update on exactly one role, reachable only via group membership.
        var specificRoleAccess = await CreatePermissionAsync(
            AccountId,
            Permissions.Constants.PermissionConstants.ROLE_RESOURCE_TYPE,
            EditorRoleId,
            read: true,
            update: true
        );
        await AssignPermissionToRoleAsync(specificRoleAccess, EditorRoleId, AccountId);

        EditorGroupId = await AsOwnerInAccountAsync(
            AccountId,
            async registration =>
            {
                var group = await registration.RegisterGroupAsync(
                    new RegisterGroupRequest("Editor Group", AccountId)
                );
                Assert.Equal(CreateGroupResultCode.Success, group.ResultCode);
                return group.CreatedGroupId;
            }
        );

        var usersToGroup = await AsOwnerInAccountAsync(
            AccountId,
            registration =>
                registration.RegisterUsersWithGroupAsync(
                    new RegisterUsersWithGroupRequest([GroupMemberUserId], EditorGroupId, AccountId)
                )
        );
        Assert.Equal(AddUsersToGroupResultCode.Success, usersToGroup.ResultCode);

        var rolesToGroup = await AsOwnerInAccountAsync(
            AccountId,
            registration =>
                registration.RegisterRolesWithGroupAsync(
                    new RegisterRolesWithGroupRequest([EditorRoleId], EditorGroupId, AccountId)
                )
        );
        Assert.Equal(AssignRolesToGroupResultCode.Success, rolesToGroup.ResultCode);
    }

    private async Task<Guid> CreateRoleAsync(string name, Guid accountId)
    {
        var result = await AsOwnerInAccountAsync(
            accountId,
            registration => registration.RegisterRoleAsync(new RegisterRoleRequest(name, accountId))
        );
        Assert.Equal(CreateRoleResultCode.Success, result.ResultCode);
        return result.CreatedRoleId;
    }

    private async Task<Guid> CreatePermissionAsync(
        Guid accountId,
        string resourceType,
        Guid resourceId,
        bool create = false,
        bool read = false,
        bool update = false,
        bool delete = false,
        bool execute = false
    )
    {
        var result = await AsOwnerInAccountAsync(
            accountId,
            registration =>
                registration.RegisterPermissionAsync(
                    new RegisterPermissionRequest(
                        accountId,
                        resourceType,
                        resourceId,
                        create,
                        read,
                        update,
                        delete,
                        execute
                    )
                )
        );
        Assert.Equal(CreatePermissionResultCode.Success, result.ResultCode);
        return result.CreatedPermissionId;
    }

    private async Task AssignPermissionToRoleAsync(Guid permissionId, Guid roleId, Guid accountId)
    {
        var result = await AsOwnerInAccountAsync(
            accountId,
            registration =>
                registration.RegisterPermissionsWithRoleAsync(
                    new RegisterPermissionsWithRoleRequest([permissionId], roleId, accountId)
                )
        );
        Assert.Equal(AssignPermissionsToRoleResultCode.Success, result.ResultCode);
    }

    private async Task AssignRoleToUserAsync(Guid roleId, Guid userId, Guid accountId)
    {
        var result = await AsOwnerInAccountAsync(
            accountId,
            registration =>
                registration.RegisterRolesWithUserAsync(
                    new RegisterRolesWithUserRequest([roleId], userId, accountId)
                )
        );
        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);
    }
}

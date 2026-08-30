using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.IntegrationTests.Authorization;

/// <summary>
/// I2 - the authorization matrix, the highest-risk surface in the library.
///
/// Every case runs against real EF and real SQL, because what is under test is whether a grant
/// survives traversal across the M:M join entities to produce the right decision. A mock repo
/// would only confirm that the mock agrees with itself.
///
/// The denials matter more than the allowances. A permission model that grants correctly but
/// fails to deny is not a permission model.
/// </summary>
public class AuthorizationMatrixTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    // --- wildcard grants ---------------------------------------------------------------------

    [Fact]
    public async Task WildcardGrant_AllowsAccessToAnyResourceOfThatType()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.True(authorized, "A wildcard (Guid.Empty) grant must cover every resource id.");
    }

    [Fact]
    public async Task WildcardGrant_CoversAResourceIdThatDoesNotExist()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            Guid.NewGuid()
        );

        Assert.True(authorized);
    }

    // --- operation isolation -----------------------------------------------------------------

    [Theory]
    [InlineData(AuthAction.Create)]
    [InlineData(AuthAction.Update)]
    [InlineData(AuthAction.Delete)]
    [InlineData(AuthAction.Execute)]
    public async Task AGrantOnOneOperation_DoesNotImplyAnother(AuthAction action)
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            action,
            PermissionConstants.USER_RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.False(authorized, $"Read must not imply {action}.");
    }

    // --- resource type isolation -------------------------------------------------------------

    [Fact]
    public async Task AGrantOnOneResourceType_DoesNotLeakToAnother()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.GROUP_RESOURCE_TYPE,
            scenario.EditorGroupId
        );

        Assert.False(authorized, "A grant on users must not confer anything on groups.");
    }

    // --- grants through a group ---------------------------------------------------------------

    [Fact]
    public async Task PermissionsResolveThroughGroupToRole()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.True(authorized, "Group membership must confer the group's roles' permissions.");
    }

    [Fact]
    public async Task GroupGrantedUpdateResolvesToo()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Update,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.True(authorized);
    }

    [Fact]
    public async Task SpecificResourceGrant_DoesNotLeakToSiblingResources()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.UngrantedRoleId
        );

        Assert.False(authorized, "A grant on one role must not confer access to a different role.");
    }

    [Fact]
    public async Task SpecificResourceGrant_DoesNotConferUngrantedOperations()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Delete,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.False(authorized);
    }

    [Fact]
    public async Task AUserOutsideTheGroup_DoesNotInheritItsGrants()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Update,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.False(authorized, "Only group members may inherit the group's roles.");
    }

    // --- multiple resource ids ----------------------------------------------------------------

    [Fact]
    public async Task AllRequestedResourcesMustBeGranted_NotJustOne()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId,
            scenario.UngrantedRoleId
        );

        Assert.False(
            authorized,
            "A batch check must fail when any single resource in it is not granted."
        );
    }

    // --- account owner ------------------------------------------------------------------------

    [Fact]
    public async Task AccountOwner_IsAuthorizedWithoutExplicitGrants()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            AuthAction.Delete,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.UngrantedRoleId
        );

        Assert.True(authorized, "The owner role is expected to carry full access.");
    }

    // --- cross-account isolation ---------------------------------------------------------------

    [Fact]
    public async Task AGrantInOneAccount_ConfersNothingInAnother()
    {
        // The owner belongs to both accounts, so this isolates the account scope rather than
        // membership: the grant lives in the primary account only.
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            null,
            AuthAction.Read,
            PermissionConstants.USER_RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.False(
            authorized,
            "Without an account context the account-scoped grant must not apply."
        );
    }

    // --- no membership -------------------------------------------------------------------------

    [Theory]
    [InlineData(AuthAction.Create)]
    [InlineData(AuthAction.Read)]
    [InlineData(AuthAction.Update)]
    [InlineData(AuthAction.Delete)]
    [InlineData(AuthAction.Execute)]
    public async Task AUserWithNoMembership_IsDeniedEverything(AuthAction action)
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.OutsiderUsername,
            null,
            action,
            PermissionConstants.USER_RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.False(authorized);
    }

    [Fact]
    public async Task AUserWithNoMembership_CannotReachAnotherAccountsRoles()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.OutsiderUsername,
            null,
            AuthAction.Read,
            PermissionConstants.ROLE_RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.False(authorized);
    }
}

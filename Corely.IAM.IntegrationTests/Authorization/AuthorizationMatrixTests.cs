using Corely.IAM.Groups.Models;
using Corely.IAM.IntegrationTests.Infrastructure;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;

namespace Corely.IAM.IntegrationTests.Authorization;

public class AuthorizationMatrixTests(IamScenario scenario) : IClassFixture<IamScenario>
{
    [Fact]
    public async Task WildcardGrant_AllowsAccessToAnyResourceOfThatType()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            User.RESOURCE_TYPE,
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
            User.RESOURCE_TYPE,
            Guid.NewGuid()
        );

        Assert.True(authorized);
    }

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
            User.RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.False(authorized, $"Read must not imply {action}.");
    }

    [Fact]
    public async Task AGrantOnOneResourceType_DoesNotLeakToAnother()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            Group.RESOURCE_TYPE,
            scenario.EditorGroupId
        );

        Assert.False(authorized, "A grant on users must not confer anything on groups.");
    }

    [Fact]
    public async Task PermissionsResolveThroughGroupToRole()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            Role.RESOURCE_TYPE,
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
            Role.RESOURCE_TYPE,
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
            Role.RESOURCE_TYPE,
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
            Role.RESOURCE_TYPE,
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
            Role.RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.False(authorized, "Only group members may inherit the group's roles.");
    }

    [Fact]
    public async Task AllRequestedResourcesMustBeGranted_NotJustOne()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.GroupMemberUsername,
            scenario.AccountId,
            AuthAction.Read,
            Role.RESOURCE_TYPE,
            scenario.EditorRoleId,
            scenario.UngrantedRoleId
        );

        Assert.False(
            authorized,
            "A batch check must fail when any single resource in it is not granted."
        );
    }

    [Fact]
    public async Task AccountOwner_IsAuthorizedWithoutExplicitGrants()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.OwnerUsername,
            scenario.AccountId,
            AuthAction.Delete,
            Role.RESOURCE_TYPE,
            scenario.UngrantedRoleId
        );

        Assert.True(authorized, "The owner role is expected to carry full access.");
    }

    [Fact]
    public async Task AGrantInOneAccount_ConfersNothingInAnother()
    {
        var authorized = await scenario.IsAuthorizedAsync(
            scenario.DirectMemberUsername,
            null,
            AuthAction.Read,
            User.RESOURCE_TYPE,
            scenario.OwnerUserId
        );

        Assert.False(
            authorized,
            "Without an account context the account-scoped grant must not apply."
        );
    }

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
            User.RESOURCE_TYPE,
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
            Role.RESOURCE_TYPE,
            scenario.EditorRoleId
        );

        Assert.False(authorized);
    }
}

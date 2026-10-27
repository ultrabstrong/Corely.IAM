using Corely.IAM.Permissions.Models;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class PermissionExtensionsTests
{
    [Fact]
    public void ActionsText_ListsTheGrantedActionsInCrudxOrder_ForSomeActions() =>
        Assert.Equal(
            "Create, Read, Execute",
            new Permission
            {
                Execute = true,
                Read = true,
                Create = true,
            }.ActionsText()
        );

    [Fact]
    public void ActionsText_SaysNoActions_ForAPermissionWithoutAny() =>
        Assert.Equal(PermissionExtensions.NO_ACTIONS, new Permission().ActionsText());

    [Fact]
    public void ScopeText_IsEveryResource_ForTheEmptyResourceId() =>
        Assert.Equal("every resource", new Permission { ResourceId = Guid.Empty }.ScopeText());

    [Fact]
    public void ScopeText_IsTheResourceId_ForASpecificResource()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(id.ToString(), new Permission { ResourceId = id }.ScopeText());
    }
}

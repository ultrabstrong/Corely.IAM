using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Web.Components.ApiKeys;

namespace Corely.IAM.Web.UnitTests.Components.ApiKeys;

public class ApiKeyAccessTests
{
    private static ApiKey KeyGranting(params string[] types) =>
        new()
        {
            Name = "ci",
            EffectivePermissions =
            [
                .. types.Select(t => new EffectivePermission { ResourceType = t, Read = true }),
            ],
        };

    [Fact]
    public void Of_MarksATypeIgnored_ForATypeThePlatformDoesNotAllow()
    {
        var access = ApiKeyAccess.Of(
            KeyGranting("report", "secret"),
            new ResourceTypeSet(["report"])
        );

        Assert.True(access.HasUsableAccess);
        Assert.True(access.Types.Single(t => t.ResourceType == "report").Allowed);
        Assert.False(access.Types.Single(t => t.ResourceType == "secret").Allowed);
    }

    [Fact]
    public void Of_ListsAllowedTypesFirst_ForAMixOfBoth()
    {
        var access = ApiKeyAccess.Of(KeyGranting("aaa", "zzz"), new ResourceTypeSet(["zzz"]));

        Assert.Equal(["zzz", "aaa"], access.Types.Select(t => t.ResourceType));
    }

    [Fact]
    public void Of_HasNoUsableAccess_ForAKeyWhoseGrantsAreAllIgnored()
    {
        var access = ApiKeyAccess.Of(KeyGranting("secret"), new ResourceTypeSet(["report"]));

        Assert.False(access.HasUsableAccess);
    }

    [Fact]
    public void Of_HasNoUsableAccess_ForAKeyWithNoRolesOrGroups()
    {
        var access = ApiKeyAccess.Of(new ApiKey { Name = "ci" }, new ResourceTypeSet(["report"]));

        Assert.False(access.HasUsableAccess);
        Assert.Empty(access.Types);
    }

    [Fact]
    public void Of_GroupsPermissions_ForTwoPermissionsOnOneType()
    {
        var access = ApiKeyAccess.Of(
            KeyGranting("report", "report"),
            new ResourceTypeSet(["report"])
        );

        Assert.Equal(2, Assert.Single(access.Types).Permissions.Count);
    }
}

using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class ResourceTypeInfoExtensionsTests
{
    private static ResourceTypeInfo Type(string name, string title, string? group) =>
        new(name, $"About {title}", Array.Empty<AuthAction>()) { Title = title, Group = group };

    private static readonly ResourceTypeInfo[] _types =
    [
        Type("platform_security_settings", "Security", ResourceTypeInfo.PLATFORM_SETTINGS_GROUP),
        Type("quota", "Quota", "Billing"),
        Type("user", "Users", ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP),
        Type("legacy", "legacy", null),
        Type("grant", "Grants", "Billing"),
        Type("document_workflows", "Document workflows", "Documents"),
        Type("account", "Accounts", ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP),
    ];

    [Fact]
    public void Grouped_PutsIdentityFirstHostGroupsByNamePlatformSettingsThenOther_ForMixedTypes() =>
        Assert.Equal(
            [
                ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP,
                "Billing",
                "Documents",
                ResourceTypeInfo.PLATFORM_SETTINGS_GROUP,
                ResourceTypeGroup.UNGROUPED,
            ],
            _types.Grouped().Select(g => g.Name)
        );

    [Fact]
    public void Grouped_SortsTypesByTitle_ForTypesInOneGroup() =>
        Assert.Equal(["Accounts", "Users"], _types.Grouped()[0].Types.Select(t => t.Title));

    [Theory]
    [InlineData("gran", "grant")]
    [InlineData("about quota", "quota")]
    [InlineData("DOCUMENT_W", "document_workflows")]
    public void Grouped_KeepsTypesMatchingTheSearch_ForATitleDescriptionOrName(
        string search,
        string expected
    ) =>
        Assert.Equal(
            [expected],
            _types.Grouped(search).SelectMany(g => g.Types).Select(t => t.Name)
        );

    [Fact]
    public void Grouped_IsEmpty_ForASearchMatchingNothing() =>
        Assert.Empty(_types.Grouped("nothing like it"));

    [Theory]
    [InlineData(ResourceTypeInfo.IDENTITY_AND_ACCESS_GROUP, "Users")]
    [InlineData(null, "Users")]
    [InlineData("Billing", "Billing: Users")]
    public void GroupedTitle_PrefixesTheGroup_OutsideIdentityAndAccess(
        string? group,
        string expected
    ) => Assert.Equal(expected, Type("user", "Users", group).GroupedTitle);
}

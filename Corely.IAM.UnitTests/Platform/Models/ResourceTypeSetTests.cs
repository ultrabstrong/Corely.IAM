using Corely.IAM.Platform.Models;

namespace Corely.IAM.UnitTests.Platform.Models;

public class ResourceTypeSetTests
{
    [Fact]
    public void Types_AreTrimmedDistinctAndOrdered_IgnoringCase()
    {
        var set = new ResourceTypeSet([" user", "Role", "USER", "", "account"]);

        Assert.Equal(["account", "Role", "user"], set.Types);
    }

    [Fact]
    public void Equality_IgnoresOrderAndCase()
    {
        Assert.Equal(new ResourceTypeSet(["user", "role"]), new ResourceTypeSet(["ROLE", "user"]));
        Assert.Equal(
            new ResourceTypeSet(["user", "role"]).GetHashCode(),
            new ResourceTypeSet(["ROLE", "user"]).GetHashCode()
        );
        Assert.NotEqual(new ResourceTypeSet(["user"]), new ResourceTypeSet(["user", "role"]));
    }

    [Fact]
    public void Contains_IgnoresCase() =>
        Assert.True(new ResourceTypeSet(["invoice"]).Contains("INVOICE"));

    [Fact]
    public void StoredValue_RoundTrips()
    {
        var set = new ResourceTypeSet(["user", "invoice"]);

        Assert.Equal(set, ResourceTypeSet.FromStoredValue(set.ToStoredValue()));
        Assert.Equal(ResourceTypeSet.Empty, ResourceTypeSet.FromStoredValue(""));
        Assert.Equal(ResourceTypeSet.Empty, ResourceTypeSet.FromStoredValue(null));
    }

    [Fact]
    public void ChangesTo_AddingATypeWeakens_AndRemovingOneDoesNot()
    {
        var before = SecurityPolicy.Default with { ApiKeyResourceTypes = new(["user"]) };

        var added = Assert.Single(
            before.ChangesTo(before with { ApiKeyResourceTypes = new(["user", "role"]) })
        );
        var removed = Assert.Single(
            before.ChangesTo(before with { ApiKeyResourceTypes = ResourceTypeSet.Empty })
        );

        Assert.True(added.Weakens);
        Assert.False(removed.Weakens);
        Assert.Empty(before.ChangesTo(before with { ApiKeyResourceTypes = new(["USER"]) }));
    }

    [Fact]
    public void ChangesTo_TurningApiKeysOnWeakens() =>
        Assert.True(
            Assert
                .Single(
                    SecurityPolicy.Default.ChangesTo(
                        SecurityPolicy.Default with
                        {
                            ApiKeysEnabled = true,
                        }
                    )
                )
                .Weakens
        );

    [Fact]
    public void Default_HasApiKeysOff_WithNoResourceTypes()
    {
        Assert.False(SecurityPolicy.Default.ApiKeysEnabled);
        Assert.False(SecurityPolicy.Default.PlatformAccountApiKeysEnabled);
        Assert.Empty(SecurityPolicy.Default.ApiKeyResourceTypes.Types);
    }
}

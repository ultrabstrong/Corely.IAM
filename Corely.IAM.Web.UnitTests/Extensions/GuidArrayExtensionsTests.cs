using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class GuidArrayExtensionsTests
{
    private static readonly Guid _a = Guid.CreateVersion7();
    private static readonly Guid _b = Guid.CreateVersion7();

    [Fact]
    public void SameIdsAs_IsTrue_ForTheSameArrayOrBothNull()
    {
        Guid[] ids = [_a];
        Guid[]? none = null;

        Assert.True(ids.SameIdsAs(ids));
        Assert.True(none.SameIdsAs(null));
    }

    [Fact]
    public void SameIdsAs_ComparesContentsInOrder()
    {
        Guid[] ids = [_a, _b];

        Assert.True(ids.SameIdsAs([_a, _b]));
        Assert.False(ids.SameIdsAs([_b, _a]));
        Assert.False(ids.SameIdsAs([_a]));
    }

    [Fact]
    public void SameIdsAs_IsFalse_WhenOnlyOneIsNull()
    {
        Guid[] ids = [];
        Guid[]? none = null;

        Assert.False(ids.SameIdsAs(null));
        Assert.False(none.SameIdsAs(ids));
    }
}

using Corely.IAM.Permissions;

namespace Corely.IAM.UnitTests.Permissions;

public class PermissionLabelProviderTests
{
    [Fact]
    public void GetCrudxLabel_AllTrue_ReturnsAllUppercase()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(true, true, true, true, true);
        Assert.Equal("CRUDX", result);
    }

    [Fact]
    public void GetCrudxLabel_AllFalse_ReturnsAllLowercase()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, false, false, false, false);
        Assert.Equal("crudx", result);
    }

    [Fact]
    public void GetCrudxLabel_ReadOnly_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, true, false, false, false);
        Assert.Equal("cRudx", result);
    }

    [Fact]
    public void GetCrudxLabel_CreateReadUpdate_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(true, true, true, false, false);
        Assert.Equal("CRUdx", result);
    }

    [Fact]
    public void GetCrudxLabel_DeleteExecute_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, false, false, true, true);
        Assert.Equal("cruDX", result);
    }
}

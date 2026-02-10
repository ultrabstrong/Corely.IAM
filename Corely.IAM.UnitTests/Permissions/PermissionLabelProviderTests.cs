using Corely.IAM.Permissions;
using FluentAssertions;

namespace Corely.IAM.UnitTests.Permissions;

public class PermissionLabelProviderTests
{
    [Fact]
    public void GetCrudxLabel_AllTrue_ReturnsAllUppercase()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(true, true, true, true, true);
        result.Should().Be("CRUDX");
    }

    [Fact]
    public void GetCrudxLabel_AllFalse_ReturnsAllLowercase()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, false, false, false, false);
        result.Should().Be("crudx");
    }

    [Fact]
    public void GetCrudxLabel_ReadOnly_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, true, false, false, false);
        result.Should().Be("cRudx");
    }

    [Fact]
    public void GetCrudxLabel_CreateReadUpdate_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(true, true, true, false, false);
        result.Should().Be("CRUdx");
    }

    [Fact]
    public void GetCrudxLabel_DeleteExecute_ReturnsCorrectMix()
    {
        var result = PermissionLabelProvider.GetCrudxLabel(false, false, false, true, true);
        result.Should().Be("cruDX");
    }
}

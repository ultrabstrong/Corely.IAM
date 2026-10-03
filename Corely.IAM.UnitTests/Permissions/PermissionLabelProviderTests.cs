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

    [Theory]
    [InlineData(true, true, true, true, true, "Group : Full Access")]
    [InlineData(false, true, false, false, true, "Group : Read & Execute")]
    [InlineData(true, true, true, false, false, "Group : Create, Read, & Update")]
    [InlineData(false, false, false, false, false, "Group : None")]
    public void GetName_NamesResourceAndActions_ForAllResources(
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute,
        string expected
    )
    {
        Assert.Equal(
            expected,
            PermissionLabelProvider.GetName(
                "group",
                Guid.Empty,
                create,
                read,
                update,
                delete,
                execute
            )
        );
    }

    [Fact]
    public void GetName_IncludesResourceId_ForSpecificResource()
    {
        var id = Guid.CreateVersion7();

        Assert.Equal(
            $"Document Workflows {id} : Read",
            PermissionLabelProvider.GetName(
                "document_workflows",
                id,
                false,
                true,
                false,
                false,
                false
            )
        );
    }
}

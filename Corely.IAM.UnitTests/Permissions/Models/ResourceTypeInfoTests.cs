using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.UnitTests.Permissions.Models;

public class ResourceTypeInfoTests
{
    [Theory]
    [InlineData(
        new[]
        {
            AuthAction.Create,
            AuthAction.Read,
            AuthAction.Update,
            AuthAction.Delete,
            AuthAction.Execute,
        },
        "Group : Full Access"
    )]
    [InlineData(new[] { AuthAction.Read }, "Group : Read")]
    [InlineData(new[] { AuthAction.Execute, AuthAction.Read }, "Group : Read & Execute")]
    [InlineData(
        new[] { AuthAction.Update, AuthAction.Create, AuthAction.Read },
        "Group : Create, Read, & Update"
    )]
    public void OwnerDefaultDescription_NamesTypeAndActions_ForOwnerActions(
        AuthAction[] ownerActions,
        string expected
    )
    {
        var type = new ResourceTypeInfo("group", "Groups", ownerActions);

        Assert.Equal(expected, type.OwnerDefaultDescription());
    }

    [Fact]
    public void OwnerDefaultDescription_TitleCasesTypeName_ForMultiWordType()
    {
        var type = new ResourceTypeInfo(
            "document_workflows",
            "Document workflows",
            [AuthAction.Read]
        );

        Assert.Equal("Document Workflows : Read", type.OwnerDefaultDescription());
    }
}

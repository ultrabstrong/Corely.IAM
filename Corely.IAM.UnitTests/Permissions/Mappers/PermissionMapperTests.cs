using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.UnitTests.Permissions.Mappers;

public class PermissionMapperTests
{
    [Fact]
    public void ToPermission_ShouldMapAllProperties()
    {
        var request = new CreatePermissionRequest(
            OwnerAccountId: Guid.CreateVersion7(),
            ResourceType: "TestResource",
            ResourceId: Guid.CreateVersion7(),
            Create: true,
            Read: true,
            Update: false,
            Delete: false,
            Execute: true,
            Description: "Test Description"
        );

        var result = request.ToPermission();

        Assert.NotNull(result);
        Assert.Equal(request.OwnerAccountId, result.AccountId);
        Assert.Equal(request.ResourceType, result.ResourceType);
        Assert.Equal(request.ResourceId, result.ResourceId);
        Assert.True(result.Create);
        Assert.True(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.True(result.Execute);
        Assert.Equal(request.Description, result.Description);
    }

    [Fact]
    public void ToPermission_ShouldSetDefaultValues()
    {
        var request = new CreatePermissionRequest(
            OwnerAccountId: Guid.CreateVersion7(),
            ResourceType: "TestResource",
            ResourceId: Guid.CreateVersion7()
        );

        var result = request.ToPermission();

        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Description);
        Assert.False(result.Create);
        Assert.False(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.False(result.Execute);
    }

    [Theory]
    [InlineData("User", true, false, false, false, false)]
    [InlineData("Document", false, true, false, false, false)]
    [InlineData("", false, false, false, false, false)]
    public void ToPermission_ShouldMapVariousInputs(
        string resourceType,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        var accountId = Guid.CreateVersion7();
        var resourceId = Guid.CreateVersion7();
        var request = new CreatePermissionRequest(
            OwnerAccountId: accountId,
            ResourceType: resourceType,
            ResourceId: resourceId,
            Create: create,
            Read: read,
            Update: update,
            Delete: delete,
            Execute: execute
        );

        var result = request.ToPermission();

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(resourceType, result.ResourceType);
        Assert.Equal(resourceId, result.ResourceId);
        Assert.Equal(create, result.Create);
        Assert.Equal(read, result.Read);
        Assert.Equal(update, result.Update);
        Assert.Equal(delete, result.Delete);
        Assert.Equal(execute, result.Execute);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var permission = new Permission
        {
            Id = Guid.CreateVersion7(),
            Description = "Test Description",
            AccountId = Guid.CreateVersion7(),
            ResourceType = "TestResource",
            ResourceId = Guid.CreateVersion7(),
            Create = true,
            Read = true,
            Update = false,
            Delete = false,
            Execute = true,
        };

        var result = permission.ToEntity();

        Assert.NotNull(result);
        Assert.Equal(permission.Id, result.Id);
        Assert.Equal(permission.Description, result.Description);
        Assert.Equal(permission.AccountId, result.AccountId);
        Assert.Equal(permission.ResourceType, result.ResourceType);
        Assert.Equal(permission.ResourceId, result.ResourceId);
        Assert.True(result.Create);
        Assert.True(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.True(result.Execute);
        Assert.Null(result.Account);
        Assert.Null(result.Roles);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new PermissionEntity
        {
            Id = Guid.CreateVersion7(),
            Description = "Test Description",
            AccountId = Guid.CreateVersion7(),
            ResourceType = "TestResource",
            ResourceId = Guid.CreateVersion7(),
            Create = true,
            Read = true,
            Update = false,
            Delete = false,
            Execute = true,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel();

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Description, result.Description);
        Assert.Equal(entity.AccountId, result.AccountId);
        Assert.Equal(entity.ResourceType, result.ResourceType);
        Assert.Equal(entity.ResourceId, result.ResourceId);
        Assert.True(result.Create);
        Assert.True(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.True(result.Execute);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        var originalPermission = new Permission
        {
            Id = Guid.CreateVersion7(),
            Description = "Round trip test",
            AccountId = Guid.CreateVersion7(),
            ResourceType = "TestResource",
            ResourceId = Guid.CreateVersion7(),
            Create = true,
            Read = false,
            Update = true,
            Delete = false,
            Execute = true,
        };

        var entity = originalPermission.ToEntity();
        var resultPermission = entity.ToModel();

        Assert.Equal(originalPermission.Id, resultPermission.Id);
        Assert.Equal(originalPermission.Description, resultPermission.Description);
        Assert.Equal(originalPermission.AccountId, resultPermission.AccountId);
        Assert.Equal(originalPermission.ResourceType, resultPermission.ResourceType);
        Assert.Equal(originalPermission.ResourceId, resultPermission.ResourceId);
        Assert.Equal(originalPermission.Create, resultPermission.Create);
        Assert.Equal(originalPermission.Read, resultPermission.Read);
        Assert.Equal(originalPermission.Update, resultPermission.Update);
        Assert.Equal(originalPermission.Delete, resultPermission.Delete);
        Assert.Equal(originalPermission.Execute, resultPermission.Execute);
    }

    [Theory]
    [InlineData("Admin permission", "User", true, true, true, true, true)]
    [InlineData(null, "Document", false, true, false, false, false)]
    [InlineData("", "", false, false, false, false, false)]
    public void ToEntity_ShouldMapVariousInputs(
        string? description,
        string resourceType,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        var permissionId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        var resourceId = Guid.CreateVersion7();

        var permission = new Permission
        {
            Id = permissionId,
            Description = description,
            AccountId = accountId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Create = create,
            Read = read,
            Update = update,
            Delete = delete,
            Execute = execute,
        };

        var result = permission.ToEntity();

        Assert.Equal(permissionId, result.Id);
        Assert.Equal(description, result.Description);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(resourceType, result.ResourceType);
        Assert.Equal(resourceId, result.ResourceId);
        Assert.Equal(create, result.Create);
        Assert.Equal(read, result.Read);
        Assert.Equal(update, result.Update);
        Assert.Equal(delete, result.Delete);
        Assert.Equal(execute, result.Execute);
    }

    [Theory]
    [InlineData("Admin permission", "User", true, true, true, true, true)]
    [InlineData(null, "Document", false, true, false, false, false)]
    [InlineData("", "", false, false, false, false, false)]
    public void ToModel_ShouldMapVariousInputs(
        string? description,
        string resourceType,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        var permissionId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        var resourceId = Guid.CreateVersion7();

        var entity = new PermissionEntity
        {
            Id = permissionId,
            Description = description,
            AccountId = accountId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Create = create,
            Read = read,
            Update = update,
            Delete = delete,
            Execute = execute,
        };

        var result = entity.ToModel();

        Assert.Equal(permissionId, result.Id);
        Assert.Equal(description, result.Description);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(resourceType, result.ResourceType);
        Assert.Equal(resourceId, result.ResourceId);
        Assert.Equal(create, result.Create);
        Assert.Equal(read, result.Read);
        Assert.Equal(update, result.Update);
        Assert.Equal(delete, result.Delete);
        Assert.Equal(execute, result.Execute);
    }

    [Fact]
    public void DisplayName_ShouldFormatCorrectly_WhenResourceIdIsZero()
    {
        var permission = new Permission
        {
            ResourceType = "group",
            ResourceId = Guid.Empty,
            Create = true,
            Read = true,
            Update = false,
            Delete = false,
            Execute = false,
        };

        Assert.Equal("group - all CRudx", permission.DisplayName);
    }

    [Fact]
    public void DisplayName_ShouldFormatCorrectly_WhenResourceIdIsSpecific()
    {
        var permission = new Permission
        {
            ResourceType = "group",
            ResourceId = Guid.CreateVersion7(),
            Create = false,
            Read = true,
            Update = false,
            Delete = false,
            Execute = false,
        };

        Assert.Equal($"group - {permission.ResourceId} cRudx", permission.DisplayName);
    }

    [Fact]
    public void DisplayName_ShouldShowAllCrudxFlags()
    {
        var permission = new Permission
        {
            ResourceType = "user",
            ResourceId = Guid.Empty,
            Create = true,
            Read = true,
            Update = true,
            Delete = true,
            Execute = true,
        };

        Assert.Equal("user - all CRUDX", permission.DisplayName);
    }

    [Theory]
    [InlineData(AuthAction.Create)]
    [InlineData(AuthAction.Read)]
    [InlineData(AuthAction.Update)]
    [InlineData(AuthAction.Delete)]
    [InlineData(AuthAction.Execute)]
    public void Allows_ReadsOnlyTheFlagForTheAction(AuthAction action)
    {
        var only = new PermissionEntity
        {
            Create = action == AuthAction.Create,
            Read = action == AuthAction.Read,
            Update = action == AuthAction.Update,
            Delete = action == AuthAction.Delete,
            Execute = action == AuthAction.Execute,
        };

        Assert.True(only.Allows(action));
        Assert.All(
            Enum.GetValues<AuthAction>().Where(a => a != action),
            other => Assert.False(only.Allows(other))
        );
    }

    [Fact]
    public void Allows_IsFalse_ForAnUnknownAction()
    {
        var all = OwnerSystemPermission();

        Assert.False(all.Allows((AuthAction)999));
    }

    [Fact]
    public void IsOwnerSystemPermission_IsTrue_ForSystemWildcardWithEveryAction()
    {
        Assert.True(OwnerSystemPermission().IsOwnerSystemPermission());
    }

    public static TheoryData<string> OwnerPermissionVariants() =>
        [
            "notSystem",
            "resourceType",
            "resourceId",
            "create",
            "read",
            "update",
            "delete",
            "execute",
        ];

    [Theory]
    [MemberData(nameof(OwnerPermissionVariants))]
    public void IsOwnerSystemPermission_IsFalse_WhenAnyPartDiffers(string variant)
    {
        var p = OwnerSystemPermission();
        switch (variant)
        {
            case "notSystem":
                p.IsSystemDefined = false;
                break;
            case "resourceType":
                p.ResourceType = "group";
                break;
            case "resourceId":
                p.ResourceId = Guid.CreateVersion7();
                break;
            case "create":
                p.Create = false;
                break;
            case "read":
                p.Read = false;
                break;
            case "update":
                p.Update = false;
                break;
            case "delete":
                p.Delete = false;
                break;
            case "execute":
                p.Execute = false;
                break;
        }

        Assert.False(p.IsOwnerSystemPermission());
    }

    private static PermissionEntity OwnerSystemPermission() =>
        new()
        {
            IsSystemDefined = true,
            ResourceType = PermissionConstants.ALL_RESOURCE_TYPES,
            ResourceId = Guid.Empty,
            Create = true,
            Read = true,
            Update = true,
            Delete = true,
            Execute = true,
        };
}

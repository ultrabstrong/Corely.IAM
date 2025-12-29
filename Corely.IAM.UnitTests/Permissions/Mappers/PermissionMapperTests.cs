using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Permissions.Models;

namespace Corely.IAM.UnitTests.Permissions.Mappers;

public class PermissionMapperTests
{
    [Fact]
    public void ToPermission_ShouldMapAllProperties()
    {
        // Arrange
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

        // Act
        var result = request.ToPermission();

        // Assert
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
        // Arrange
        var request = new CreatePermissionRequest(
            OwnerAccountId: Guid.CreateVersion7(),
            ResourceType: "TestResource",
            ResourceId: Guid.CreateVersion7()
        );

        // Act
        var result = request.ToPermission();

        // Assert
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
        // Arrange
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

        // Act
        var result = request.ToPermission();

        // Assert
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
        // Arrange
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

        // Act
        var result = permission.ToEntity();

        // Assert
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
        // Arrange
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

        // Act
        var result = entity.ToModel();

        // Assert
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
        // Arrange
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

        // Act
        var entity = originalPermission.ToEntity();
        var resultPermission = entity.ToModel();

        // Assert
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

        // Arrange
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

        // Act
        var result = permission.ToEntity();

        // Assert
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

        // Arrange
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

        // Act
        var result = entity.ToModel();

        // Assert
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
        // Arrange
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

        // Act & Assert
        Assert.Equal("group - all CRudx", permission.DisplayName);
    }

    [Fact]
    public void DisplayName_ShouldFormatCorrectly_WhenResourceIdIsSpecific()
    {
        // Arrange
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

        // Act & Assert
        Assert.Equal("group - 42 cRudx", permission.DisplayName);
    }

    [Fact]
    public void DisplayName_ShouldShowAllCrudxFlags()
    {
        // Arrange
        var permission = new Permission
        {
            ResourceType = "user",
            ResourceId = Guid.CreateVersion7(),
            Create = true,
            Read = true,
            Update = true,
            Delete = true,
            Execute = true,
        };

        // Act & Assert
        Assert.Equal("user - all CRUDX", permission.DisplayName);
    }
}

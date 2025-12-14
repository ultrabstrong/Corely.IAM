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
            OwnerAccountId: 123,
            ResourceType: "TestResource",
            ResourceId: 456,
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
        Assert.Equal(123, result.AccountId);
        Assert.Equal("TestResource", result.ResourceType);
        Assert.Equal(456, result.ResourceId);
        Assert.True(result.Create);
        Assert.True(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.True(result.Execute);
        Assert.Equal("Test Description", result.Description);
    }

    [Fact]
    public void ToPermission_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreatePermissionRequest(
            OwnerAccountId: 123,
            ResourceType: "TestResource",
            ResourceId: 456
        );

        // Act
        var result = request.ToPermission();

        // Assert
        Assert.Equal(0, result.Id);
        Assert.Null(result.Description);
        Assert.False(result.Create);
        Assert.False(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.False(result.Execute);
    }

    [Theory]
    [InlineData(1, "User", 100, true, false, false, false, false)]
    [InlineData(2, "Document", 200, false, true, false, false, false)]
    [InlineData(0, "", 0, false, false, false, false, false)]
    public void ToPermission_ShouldMapVariousInputs(
        int accountId,
        string resourceType,
        int resourceId,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
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
            Id = 42,
            Description = "Test Description",
            AccountId = 123,
            ResourceType = "TestResource",
            ResourceId = 456,
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
        Assert.Equal(42, result.Id);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(123, result.AccountId);
        Assert.Equal("TestResource", result.ResourceType);
        Assert.Equal(456, result.ResourceId);
        Assert.True(result.Create);
        Assert.True(result.Read);
        Assert.False(result.Update);
        Assert.False(result.Delete);
        Assert.True(result.Execute);
    }

    [Fact]
    public void ToEntity_ShouldNotMapNavigationProperties()
    {
        // Arrange
        var permission = new Permission
        {
            Id = 1,
            AccountId = 100,
            ResourceType = "Resource",
            ResourceId = 1,
        };

        // Act
        var result = permission.ToEntity();

        // Assert
        Assert.Null(result.Account);
        Assert.Null(result.Roles);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new PermissionEntity
        {
            Id = 42,
            Description = "Test Description",
            AccountId = 123,
            ResourceType = "TestResource",
            ResourceId = 456,
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
        Assert.Equal(42, result.Id);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(123, result.AccountId);
        Assert.Equal("TestResource", result.ResourceType);
        Assert.Equal(456, result.ResourceId);
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
            Id = 99,
            Description = "Round trip test",
            AccountId = 456,
            ResourceType = "TestResource",
            ResourceId = 789,
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
    [InlineData(1, "Admin permission", 100, "User", 1, true, true, true, true, true)]
    [InlineData(2, null, 200, "Document", 2, false, true, false, false, false)]
    [InlineData(0, "", 0, "", 0, false, false, false, false, false)]
    public void ToEntity_ShouldMapVariousInputs(
        int id,
        string? description,
        int accountId,
        string resourceType,
        int resourceId,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        // Arrange
        var permission = new Permission
        {
            Id = id,
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
        Assert.Equal(id, result.Id);
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
    [InlineData(1, "Admin permission", 100, "User", 1, true, true, true, true, true)]
    [InlineData(2, null, 200, "Document", 2, false, true, false, false, false)]
    [InlineData(0, "", 0, "", 0, false, false, false, false, false)]
    public void ToModel_ShouldMapVariousInputs(
        int id,
        string? description,
        int accountId,
        string resourceType,
        int resourceId,
        bool create,
        bool read,
        bool update,
        bool delete,
        bool execute
    )
    {
        // Arrange
        var entity = new PermissionEntity
        {
            Id = id,
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
        Assert.Equal(id, result.Id);
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
            ResourceId = 0,
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
            ResourceId = 42,
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
            ResourceId = 0,
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

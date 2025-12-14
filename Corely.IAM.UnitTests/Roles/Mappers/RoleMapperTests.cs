using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Mappers;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.UnitTests.Roles.Mappers;

public class RoleMapperTests
{
    [Fact]
    public void ToRole_ShouldMapAllProperties()
    {
        // Arrange
        var request = new CreateRoleRequest(RoleName: "TestRole", OwnerAccountId: 123);

        // Act
        var result = request.ToRole();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestRole", result.Name);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToRole_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreateRoleRequest(RoleName: "TestRole", OwnerAccountId: 123);

        // Act
        var result = request.ToRole();

        // Assert
        Assert.Equal(0, result.Id);
        Assert.Null(result.Description);
        Assert.False(result.IsSystemDefined);
    }

    [Theory]
    [InlineData("Admin", 1)]
    [InlineData("User", 999)]
    [InlineData("", 0)]
    public void ToRole_ShouldMapVariousInputs(string roleName, int accountId)
    {
        // Arrange
        var request = new CreateRoleRequest(RoleName: roleName, OwnerAccountId: accountId);

        // Act
        var result = request.ToRole();

        // Assert
        Assert.Equal(roleName, result.Name);
        Assert.Equal(accountId, result.AccountId);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var role = new Role
        {
            Id = 42,
            Name = "TestRole",
            Description = "Test Description",
            IsSystemDefined = true,
            AccountId = 123,
        };

        // Act
        var result = role.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("TestRole", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.True(result.IsSystemDefined);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToEntity_ShouldNotMapNavigationProperties()
    {
        // Arrange
        var role = new Role
        {
            Id = 1,
            Name = "Test",
            AccountId = 100,
        };

        // Act
        var result = role.ToEntity();

        // Assert
        Assert.Null(result.Account);
        Assert.Null(result.Users);
        Assert.Null(result.Groups);
        Assert.Null(result.Permissions);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new RoleEntity
        {
            Id = 42,
            Name = "TestRole",
            Description = "Test Description",
            IsSystemDefined = true,
            AccountId = 123,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("TestRole", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.True(result.IsSystemDefined);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalRole = new Role
        {
            Id = 99,
            Name = "RoundTripRole",
            Description = "Round trip test",
            IsSystemDefined = false,
            AccountId = 456,
        };

        // Act
        var entity = originalRole.ToEntity();
        var resultRole = entity.ToModel();

        // Assert
        Assert.Equal(originalRole.Id, resultRole.Id);
        Assert.Equal(originalRole.Name, resultRole.Name);
        Assert.Equal(originalRole.Description, resultRole.Description);
        Assert.Equal(originalRole.IsSystemDefined, resultRole.IsSystemDefined);
        Assert.Equal(originalRole.AccountId, resultRole.AccountId);
    }

    [Theory]
    [InlineData(1, "Admin", "Admin role", true, 100)]
    [InlineData(2, "User", null, false, 200)]
    [InlineData(0, "", "", false, 0)]
    public void ToEntity_ShouldMapVariousInputs(
        int id,
        string name,
        string? description,
        bool isSystemDefined,
        int accountId
    )
    {
        // Arrange
        var role = new Role
        {
            Id = id,
            Name = name,
            Description = description,
            IsSystemDefined = isSystemDefined,
            AccountId = accountId,
        };

        // Act
        var result = role.ToEntity();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(isSystemDefined, result.IsSystemDefined);
        Assert.Equal(accountId, result.AccountId);
    }

    [Theory]
    [InlineData(1, "Admin", "Admin role", true, 100)]
    [InlineData(2, "User", null, false, 200)]
    [InlineData(0, "", "", false, 0)]
    public void ToModel_ShouldMapVariousInputs(
        int id,
        string name,
        string? description,
        bool isSystemDefined,
        int accountId
    )
    {
        // Arrange
        var entity = new RoleEntity
        {
            Id = id,
            Name = name,
            Description = description,
            IsSystemDefined = isSystemDefined,
            AccountId = accountId,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(isSystemDefined, result.IsSystemDefined);
        Assert.Equal(accountId, result.AccountId);
    }
}

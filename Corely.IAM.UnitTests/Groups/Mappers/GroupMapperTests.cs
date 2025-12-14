using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Mappers;
using Corely.IAM.Groups.Models;

namespace Corely.IAM.UnitTests.Groups.Mappers;

public class GroupMapperTests
{
    [Fact]
    public void ToGroup_ShouldMapAllProperties()
    {
        // Arrange
        var request = new CreateGroupRequest(GroupName: "TestGroup", OwnerAccountId: 123);

        // Act
        var result = request.ToGroup();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestGroup", result.Name);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToGroup_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreateGroupRequest(GroupName: "TestGroup", OwnerAccountId: 123);

        // Act
        var result = request.ToGroup();

        // Assert
        Assert.Equal(0, result.Id);
        Assert.Null(result.Description);
    }

    [Theory]
    [InlineData("Admins", 1)]
    [InlineData("Users", 999)]
    [InlineData("", 0)]
    public void ToGroup_ShouldMapVariousInputs(string groupName, int accountId)
    {
        // Arrange
        var request = new CreateGroupRequest(GroupName: groupName, OwnerAccountId: accountId);

        // Act
        var result = request.ToGroup();

        // Assert
        Assert.Equal(groupName, result.Name);
        Assert.Equal(accountId, result.AccountId);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var group = new Group
        {
            Id = 42,
            Name = "TestGroup",
            Description = "Test Description",
            AccountId = 123,
        };

        // Act
        var result = group.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("TestGroup", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToEntity_ShouldNotMapNavigationProperties()
    {
        // Arrange
        var group = new Group
        {
            Id = 1,
            Name = "Test",
            AccountId = 100,
        };

        // Act
        var result = group.ToEntity();

        // Assert
        Assert.Null(result.Account);
        Assert.Null(result.Users);
        Assert.Null(result.Roles);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new GroupEntity
        {
            Id = 42,
            Name = "TestGroup",
            Description = "Test Description",
            AccountId = 123,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("TestGroup", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(123, result.AccountId);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalGroup = new Group
        {
            Id = 99,
            Name = "RoundTripGroup",
            Description = "Round trip test",
            AccountId = 456,
        };

        // Act
        var entity = originalGroup.ToEntity();
        var resultGroup = entity.ToModel();

        // Assert
        Assert.Equal(originalGroup.Id, resultGroup.Id);
        Assert.Equal(originalGroup.Name, resultGroup.Name);
        Assert.Equal(originalGroup.Description, resultGroup.Description);
        Assert.Equal(originalGroup.AccountId, resultGroup.AccountId);
    }

    [Theory]
    [InlineData(1, "Admins", "Admin group", 100)]
    [InlineData(2, "Users", null, 200)]
    [InlineData(0, "", "", 0)]
    public void ToEntity_ShouldMapVariousInputs(
        int id,
        string name,
        string? description,
        int accountId
    )
    {
        // Arrange
        var group = new Group
        {
            Id = id,
            Name = name,
            Description = description,
            AccountId = accountId,
        };

        // Act
        var result = group.ToEntity();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(accountId, result.AccountId);
    }

    [Theory]
    [InlineData(1, "Admins", "Admin group", 100)]
    [InlineData(2, "Users", null, 200)]
    [InlineData(0, "", "", 0)]
    public void ToModel_ShouldMapVariousInputs(
        int id,
        string name,
        string? description,
        int accountId
    )
    {
        // Arrange
        var entity = new GroupEntity
        {
            Id = id,
            Name = name,
            Description = description,
            AccountId = accountId,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(accountId, result.AccountId);
    }
}

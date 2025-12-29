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
        var request = new CreateGroupRequest(
            GroupName: "TestGroup",
            OwnerAccountId: Guid.CreateVersion7()
        );

        // Act
        var result = request.ToGroup();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.GroupName, result.Name);
        Assert.Equal(request.OwnerAccountId, result.AccountId);
    }

    [Fact]
    public void ToGroup_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreateGroupRequest(
            GroupName: "TestGroup",
            OwnerAccountId: Guid.CreateVersion7()
        );

        // Act
        var result = request.ToGroup();

        // Assert
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Description);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var group = new Group
        {
            Id = Guid.CreateVersion7(),
            Name = "TestGroup",
            Description = "Test Description",
            AccountId = Guid.CreateVersion7(),
        };

        // Act
        var result = group.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal(group.Name, result.Name);
        Assert.Equal(group.Description, result.Description);
        Assert.Equal(group.AccountId, result.AccountId);
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
            Id = Guid.CreateVersion7(),
            Name = "TestGroup",
            Description = "Test Description",
            AccountId = Guid.CreateVersion7(),
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Name, result.Name);
        Assert.Equal(entity.Description, result.Description);
        Assert.Equal(entity.AccountId, result.AccountId);
    }
}

using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Mappers;
using Corely.IAM.Roles.Models;

namespace Corely.IAM.UnitTests.Roles.Mappers;

public class RoleMapperTests
{
    [Fact]
    public void ToRole_ShouldMapAllProperties()
    {
        var request = new CreateRoleRequest(
            RoleName: "TestRole",
            OwnerAccountId: Guid.CreateVersion7()
        );

        var result = request.ToRole();

        Assert.NotNull(result);
        Assert.Equal("TestRole", result.Name);
        Assert.Equal(request.OwnerAccountId, result.AccountId);
    }

    [Fact]
    public void ToRole_ShouldSetDefaultValues()
    {
        var request = new CreateRoleRequest(
            RoleName: "TestRole",
            OwnerAccountId: Guid.CreateVersion7()
        );

        var result = request.ToRole();

        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.Description);
        Assert.False(result.IsSystemDefined);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            Name = "TestRole",
            Description = "Test Description",
            IsSystemDefined = true,
            AccountId = Guid.CreateVersion7(),
        };

        var result = role.ToEntity();

        Assert.NotNull(result);
        Assert.Equal(role.Id, result.Id);
        Assert.Equal(role.Name, result.Name);
        Assert.Equal(role.Description, result.Description);
        Assert.True(result.IsSystemDefined);
        Assert.Equal(role.AccountId, result.AccountId);
        Assert.Null(result.Account);
        Assert.Null(result.Users);
        Assert.Null(result.Groups);
        Assert.Null(result.Permissions);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        var entity = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = "TestRole",
            Description = "Test Description",
            IsSystemDefined = true,
            AccountId = Guid.CreateVersion7(),
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel();

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Name, result.Name);
        Assert.Equal(entity.Description, result.Description);
        Assert.True(result.IsSystemDefined);
        Assert.Equal(entity.AccountId, result.AccountId);
    }
}

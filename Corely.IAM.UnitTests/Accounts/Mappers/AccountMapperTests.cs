using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;

namespace Corely.IAM.UnitTests.Accounts.Mappers;

public class AccountMapperTests
{
    [Fact]
    public void ToAccount_ShouldMapAllProperties()
    {
        // Arrange
        var request = new CreateAccountRequest(
            AccountName: "TestAccount",
            OwnerUserId: Guid.CreateVersion7()
        );

        // Act
        var result = request.ToAccount();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestAccount", result.AccountName);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };

        // Act
        var result = account.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal("TestAccount", result.AccountName);
        Assert.Null(result.Users);
        Assert.Null(result.Groups);
        Assert.Null(result.Roles);
        Assert.Null(result.Permissions);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new AccountEntity
        {
            Id = Guid.CreateVersion7(),
            AccountName = "TestAccount",
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("TestAccount", result.AccountName);
    }
}

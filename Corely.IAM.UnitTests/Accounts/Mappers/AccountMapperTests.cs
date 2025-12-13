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
        var request = new CreateAccountRequest(AccountName: "TestAccount", OwnerUserId: 123);

        // Act
        var result = request.ToAccount();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestAccount", result.AccountName);
    }

    [Fact]
    public void ToAccount_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreateAccountRequest(AccountName: "TestAccount", OwnerUserId: 123);

        // Act
        var result = request.ToAccount();

        // Assert
        Assert.Equal(0, result.Id);
        Assert.Equal(Guid.Empty, result.PublicId);
    }

    [Theory]
    [InlineData("Account1", 1)]
    [InlineData("Account2", 999)]
    [InlineData("", 0)]
    public void ToAccount_ShouldMapVariousInputs(string accountName, int ownerId)
    {
        // Arrange
        var request = new CreateAccountRequest(AccountName: accountName, OwnerUserId: ownerId);

        // Act
        var result = request.ToAccount();

        // Assert
        Assert.Equal(accountName, result.AccountName);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var publicId = Guid.NewGuid();
        var account = new Account
        {
            Id = 42,
            PublicId = publicId,
            AccountName = "TestAccount",
        };

        // Act
        var result = account.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(publicId, result.PublicId);
        Assert.Equal("TestAccount", result.AccountName);
    }

    [Fact]
    public void ToEntity_ShouldNotMapNavigationProperties()
    {
        // Arrange
        var account = new Account { Id = 1, AccountName = "Test" };

        // Act
        var result = account.ToEntity();

        // Assert
        Assert.Null(result.Users);
        Assert.Null(result.Groups);
        Assert.Null(result.Roles);
        Assert.Null(result.Permissions);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var publicId = Guid.NewGuid();
        var entity = new AccountEntity
        {
            Id = 42,
            PublicId = publicId,
            AccountName = "TestAccount",
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal(publicId, result.PublicId);
        Assert.Equal("TestAccount", result.AccountName);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var publicId = Guid.NewGuid();
        var originalAccount = new Account
        {
            Id = 99,
            PublicId = publicId,
            AccountName = "RoundTripAccount",
        };

        // Act
        var entity = originalAccount.ToEntity();
        var resultAccount = entity.ToModel();

        // Assert
        Assert.Equal(originalAccount.Id, resultAccount.Id);
        Assert.Equal(originalAccount.PublicId, resultAccount.PublicId);
        Assert.Equal(originalAccount.AccountName, resultAccount.AccountName);
    }

    [Theory]
    [InlineData(1, "Account1")]
    [InlineData(2, "Account2")]
    [InlineData(0, "")]
    public void ToEntity_ShouldMapVariousInputs(int id, string accountName)
    {
        // Arrange
        var publicId = Guid.NewGuid();
        var account = new Account
        {
            Id = id,
            PublicId = publicId,
            AccountName = accountName,
        };

        // Act
        var result = account.ToEntity();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(publicId, result.PublicId);
        Assert.Equal(accountName, result.AccountName);
    }

    [Theory]
    [InlineData(1, "Account1")]
    [InlineData(2, "Account2")]
    [InlineData(0, "")]
    public void ToModel_ShouldMapVariousInputs(int id, string accountName)
    {
        // Arrange
        var publicId = Guid.NewGuid();
        var entity = new AccountEntity
        {
            Id = id,
            PublicId = publicId,
            AccountName = accountName,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(publicId, result.PublicId);
        Assert.Equal(accountName, result.AccountName);
    }
}

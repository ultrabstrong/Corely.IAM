using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;

namespace Corely.IAM.UnitTests.Accounts.Mappers;

public class AccountMapperTests
{
    [Fact]
    public void ToAccount_ShouldMapAllProperties()
    {
        var request = new CreateAccountRequest(
            AccountName: "TestAccount",
            OwnerUserId: Guid.CreateVersion7()
        );

        var result = request.ToAccount();

        Assert.NotNull(result);
        Assert.Equal("TestAccount", result.AccountName);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        var account = new Account { Id = Guid.CreateVersion7(), AccountName = "TestAccount" };

        var result = account.ToEntity();

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
        var entity = new AccountEntity
        {
            Id = Guid.CreateVersion7(),
            AccountName = "TestAccount",
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
        };

        var result = entity.ToModel();

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("TestAccount", result.AccountName);
    }
}

using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorUpdateTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AccountProcessor _accountProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public AccountProcessorUpdateTests()
    {
        var userContextSetter = _serviceFactory.GetRequiredService<IUserContextSetter>();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@test.com",
        };
        var account = new Account { Id = _accountId, AccountName = "TestAccount" };
        userContextSetter.SetUserContext(new UserContext(user, account, "device1", [account]));

        _accountProcessor = new AccountProcessor(
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateAccountEntityAsync(string name, Guid? accountId = null)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var entity = new AccountEntity
        {
            Id = accountId ?? _accountId,
            AccountName = name,
            Users = [],
            Groups = [],
            Roles = [],
            Permissions = [],
        };
        return await accountRepo.CreateAsync(entity);
    }

    [Fact]
    public async Task UpdateAccountAsync_UpdatesAccountName()
    {
        await CreateAccountEntityAsync("OriginalName");

        var request = new UpdateAccountRequest(_accountId, "UpdatedName");
        var result = await _accountProcessor.UpdateAccountAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);

        var getResult = await _accountProcessor.GetAccountAsync(_accountId);
        Assert.NotNull(getResult.Account);
        Assert.Equal("UpdatedName", getResult.Account.AccountName);
    }

    [Fact]
    public async Task UpdateAccountAsync_ReturnsNotFound_WhenAccountDoesNotExist()
    {
        var request = new UpdateAccountRequest(Guid.CreateVersion7(), "SomeName");
        var result = await _accountProcessor.UpdateAccountAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateAccountAsync_ReturnsNotFound_WhenAccountNotInUserAccounts()
    {
        var otherAccountId = Guid.CreateVersion7();
        await CreateAccountEntityAsync("OtherAccount", otherAccountId);

        var request = new UpdateAccountRequest(otherAccountId, "NewName");
        var result = await _accountProcessor.UpdateAccountAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateAccountAsync_ThrowsValidation_WhenNameEmpty()
    {
        await CreateAccountEntityAsync("TestAccount");

        var request = new UpdateAccountRequest(_accountId, "");
        await Assert.ThrowsAsync<ValidationException>(() =>
            _accountProcessor.UpdateAccountAsync(request)
        );
    }
}

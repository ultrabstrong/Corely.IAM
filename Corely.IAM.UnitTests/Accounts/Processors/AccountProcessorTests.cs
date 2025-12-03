using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Security.Processors;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Accounts.Processors;

public class AccountProcessorTests
{
    private const string VALID_ACCOUNT_NAME = "accountname";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly AccountProcessor _accountProcessor;

    public AccountProcessorTests()
    {
        _accountProcessor = new AccountProcessor(
            _serviceFactory.GetRequiredService<IRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<ISecurityProcessor>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );
    }

    private async Task<int> CreateUserAsync()
    {
        var userId = _fixture.Create<int>();
        var user = new UserEntity { Id = userId };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    [Fact]
    public async Task CreateAccountAsync_Fails_WhenAccountExists()
    {
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, await CreateUserAsync());
        await _accountProcessor.CreateAccountAsync(request);

        var result = await _accountProcessor.CreateAccountAsync(request);

        Assert.Equal(CreateAccountResultCode.AccountExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateAccount_ReturnsCreateAccountResult()
    {
        var userIdOfOwner = await CreateUserAsync();
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, userIdOfOwner);

        var result = await _accountProcessor.CreateAccountAsync(request);

        Assert.Equal(CreateAccountResultCode.Success, result.ResultCode);

        // Verify account is linked to owner user id
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == result.CreatedId,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity);
        Assert.NotNull(accountEntity.Users);
        Assert.Single(accountEntity.Users);
        Assert.Equal(userIdOfOwner, accountEntity.Users.First().Id);
    }

    [Fact]
    public async Task CreateAccount_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _accountProcessor.CreateAccountAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task CreateAccount_Fails_WithInvalidUserId()
    {
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, -1);

        var result = await _accountProcessor.CreateAccountAsync(request);

        Assert.Equal(CreateAccountResultCode.UserOwnerNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateAccount_Throws_WithNullAccountName()
    {
        var request = new CreateAccountRequest(null!, -1);
        var ex = await Record.ExceptionAsync(() => _accountProcessor.CreateAccountAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public async Task GetAccountByAccountIdAsync_ReturnsNull_WhenAccountDNE()
    {
        var account = await _accountProcessor.GetAccountAsync(_fixture.Create<int>());

        Assert.Null(account);
    }

    [Fact]
    public async Task GetAccountByAccountIdAsync_ReturnsAccount_WhenAccountExists()
    {
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, await CreateUserAsync());
        var result = await _accountProcessor.CreateAccountAsync(request);

        var account = await _accountProcessor.GetAccountAsync(result.CreatedId);

        Assert.NotNull(account);
        Assert.Equal(VALID_ACCOUNT_NAME, account.AccountName);
    }

    [Fact]
    public async Task GetAccountByAccountNameAsync_ReturnsNull_WhenAccountDNE()
    {
        var account = await _accountProcessor.GetAccountAsync(_fixture.Create<string>());

        Assert.Null(account);
    }

    [Fact]
    public async Task GetAccountByAccountNameAsync_ReturnsAccount_WhenAccountExists()
    {
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, await CreateUserAsync());
        await _accountProcessor.CreateAccountAsync(request);

        var account = await _accountProcessor.GetAccountAsync(VALID_ACCOUNT_NAME);

        Assert.NotNull(account);
        Assert.Equal(VALID_ACCOUNT_NAME, account.AccountName);
    }
}

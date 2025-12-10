using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
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
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
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

    private async Task<RoleEntity> CreateOwnerRoleAsync(int accountId, params int[] userIds)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        var users = new List<UserEntity>();
        foreach (var userId in userIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                users.Add(user);
            }
        }

        var ownerRole = new RoleEntity
        {
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = users,
        };

        return await roleRepo.CreateAsync(ownerRole);
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

    [Fact]
    public async Task DeleteAccountAsync_ReturnsSuccess_WhenAccountExists()
    {
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, await CreateUserAsync());
        var createResult = await _accountProcessor.CreateAccountAsync(request);

        var result = await _accountProcessor.DeleteAccountAsync(createResult.CreatedId);

        Assert.Equal(DeleteAccountResultCode.Success, result.ResultCode);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(a => a.Id == createResult.CreatedId);
        Assert.Null(accountEntity);
    }

    [Fact]
    public async Task DeleteAccountAsync_ReturnsNotFound_WhenAccountDoesNotExist()
    {
        var result = await _accountProcessor.DeleteAccountAsync(_fixture.Create<int>());

        Assert.Equal(DeleteAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsSuccess_WhenUserAndAccountExist()
    {
        var ownerUserId = await CreateUserAsync();
        var newUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new AddUserToAccountRequest(newUserId, createAccountResult.CreatedId);
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.Success, result.ResultCode);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == createAccountResult.CreatedId,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity?.Users);
        Assert.Equal(2, accountEntity.Users.Count);
        Assert.Contains(accountEntity.Users, u => u.Id == newUserId);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var ownerUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new AddUserToAccountRequest(
            _fixture.Create<int>(),
            createAccountResult.CreatedId
        );
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var userId = await CreateUserAsync();

        var request = new AddUserToAccountRequest(userId, _fixture.Create<int>());
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsUserAlreadyInAccount_WhenUserAlreadyInAccount()
    {
        var ownerUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        // Try to add the owner user again
        var request = new AddUserToAccountRequest(ownerUserId, createAccountResult.CreatedId);
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.UserAlreadyInAccountError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _accountProcessor.AddUserToAccountAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSuccess_WhenUserIsNotOwner()
    {
        var ownerUserId = await CreateUserAsync();
        var nonOwnerUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await _accountProcessor.AddUserToAccountAsync(
            new AddUserToAccountRequest(nonOwnerUserId, createAccountResult.CreatedId)
        );

        var request = new RemoveUserFromAccountRequest(
            nonOwnerUserId,
            createAccountResult.CreatedId
        );
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.Success, result.ResultCode);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == createAccountResult.CreatedId,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity?.Users);
        Assert.Single(accountEntity.Users);
        Assert.DoesNotContain(accountEntity.Users, u => u.Id == nonOwnerUserId);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var ownerUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new RemoveUserFromAccountRequest(
            _fixture.Create<int>(),
            createAccountResult.CreatedId
        );
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var userId = await CreateUserAsync();

        var request = new RemoveUserFromAccountRequest(userId, _fixture.Create<int>());
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsUserNotInAccount_WhenUserNotInAccount()
    {
        var ownerUserId = await CreateUserAsync();
        var otherUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new RemoveUserFromAccountRequest(otherUserId, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserNotInAccountError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _accountProcessor.RemoveUserFromAccountAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSuccess_WhenOwnerRemovedButOtherOwnersExist()
    {
        var ownerUserId1 = await CreateUserAsync();
        var ownerUserId2 = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId1);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await _accountProcessor.AddUserToAccountAsync(
            new AddUserToAccountRequest(ownerUserId2, createAccountResult.CreatedId)
        );

        await CreateOwnerRoleAsync(createAccountResult.CreatedId, ownerUserId1, ownerUserId2);

        var request = new RemoveUserFromAccountRequest(ownerUserId1, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSoleOwnerError_WhenSoleOwnerRemoved()
    {
        var ownerUserId = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUserId);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await CreateOwnerRoleAsync(createAccountResult.CreatedId, ownerUserId);

        var request = new RemoveUserFromAccountRequest(ownerUserId, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserIsSoleOwnerError, result.ResultCode);
    }
}

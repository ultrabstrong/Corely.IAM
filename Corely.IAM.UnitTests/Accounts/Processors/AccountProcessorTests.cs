using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Processors;
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
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<AccountProcessor>>()
        );
    }

    private async Task<UserEntity> CreateUserAsync()
    {
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);
        return created;
    }

    private async Task<RoleEntity> CreateOwnerRoleAsync(Guid accountId, params Guid[] userIds)
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();

        var account = await accountRepo.GetAsync(a => a.Id == accountId);

        var users = new List<UserEntity>();
        foreach (var userId in userIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Accounts ??= [];
                if (!user.Accounts.Any(a => a.Id == accountId) && account != null)
                {
                    user.Accounts.Add(account);
                }
                users.Add(user);
            }
        }

        var ownerRole = new RoleEntity
        {
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = users,
            Groups = [],
        };

        return await roleRepo.CreateAsync(ownerRole);
    }

    private async Task<RoleEntity> CreateOwnerRoleWithGroupAsync(
        Guid accountId,
        Guid[] directUserIds,
        Guid[] groupUserIds
    )
    {
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();

        var account = await accountRepo.GetAsync(a => a.Id == accountId);

        var directUsers = new List<UserEntity>();
        foreach (var userId in directUserIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Accounts ??= [];
                if (!user.Accounts.Any(a => a.Id == accountId) && account != null)
                {
                    user.Accounts.Add(account);
                }
                directUsers.Add(user);
            }
        }

        var groupUsers = new List<UserEntity>();
        foreach (var userId in groupUserIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Accounts ??= [];
                if (!user.Accounts.Any(a => a.Id == accountId) && account != null)
                {
                    user.Accounts.Add(account);
                }
                groupUsers.Add(user);
            }
        }

        var group = new GroupEntity
        {
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = groupUsers,
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = directUsers,
            Groups = [createdGroup],
        };

        return await roleRepo.CreateAsync(ownerRole);
    }

    [Fact]
    public async Task CreateAccountAsync_Fails_WhenAccountExists()
    {
        var ownerUser = await CreateUserAsync();
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        await _accountProcessor.CreateAccountAsync(request);

        var result = await _accountProcessor.CreateAccountAsync(request);

        Assert.Equal(CreateAccountResultCode.AccountExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateAccount_ReturnsCreateAccountResult()
    {
        var ownerUser = await CreateUserAsync();
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);

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
        Assert.Equal(ownerUser.Id, accountEntity.Users.First().Id);
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
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, Guid.Empty);

        var result = await _accountProcessor.CreateAccountAsync(request);

        Assert.Equal(CreateAccountResultCode.UserOwnerNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateAccount_Throws_WithNullAccountName()
    {
        var request = new CreateAccountRequest(null!, Guid.Empty);
        var ex = await Record.ExceptionAsync(() => _accountProcessor.CreateAccountAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public async Task GetAccountByAccountIdAsync_ReturnsNull_WhenAccountDNE()
    {
        var result = await _accountProcessor.GetAccountAsync(Guid.CreateVersion7());

        Assert.Equal(GetAccountResultCode.AccountNotFoundError, result.ResultCode);
        Assert.Null(result.Account);
    }

    [Fact]
    public async Task GetAccountByAccountIdAsync_ReturnsAccount_WhenAccountExists()
    {
        var ownerUser = await CreateUserAsync();
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createResult = await _accountProcessor.CreateAccountAsync(request);

        var result = await _accountProcessor.GetAccountAsync(createResult.CreatedId);

        Assert.Equal(GetAccountResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Account);
        Assert.Equal(VALID_ACCOUNT_NAME, result.Account.AccountName);
    }

    [Fact]
    public async Task DeleteAccountAsync_ReturnsSuccess_WhenAccountExists()
    {
        var ownerUser = await CreateUserAsync();
        var request = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
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
        var result = await _accountProcessor.DeleteAccountAsync(Guid.CreateVersion7());

        Assert.Equal(DeleteAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsSuccess_WhenUserAndAccountExist()
    {
        var ownerUser = await CreateUserAsync();
        var newUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new AddUserToAccountRequest(newUser.Id, createAccountResult.CreatedId);
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.Success, result.ResultCode);

        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var accountEntity = await accountRepo.GetAsync(
            a => a.Id == createAccountResult.CreatedId,
            include: q => q.Include(a => a.Users)
        );
        Assert.NotNull(accountEntity?.Users);
        Assert.Equal(2, accountEntity.Users.Count);
        Assert.Contains(accountEntity.Users, u => u.Id == newUser.Id);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var ownerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new AddUserToAccountRequest(
            Guid.CreateVersion7(),
            createAccountResult.CreatedId
        );
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var user = await CreateUserAsync();

        var request = new AddUserToAccountRequest(user.Id, Guid.CreateVersion7());
        var result = await _accountProcessor.AddUserToAccountAsync(request);

        Assert.Equal(AddUserToAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUserToAccountAsync_ReturnsUserAlreadyInAccount_WhenUserAlreadyInAccount()
    {
        var ownerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        // Try to add the owner user again
        var request = new AddUserToAccountRequest(ownerUser.Id, createAccountResult.CreatedId);
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
        var ownerUser = await CreateUserAsync();
        var nonOwnerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await _accountProcessor.AddUserToAccountAsync(
            new AddUserToAccountRequest(nonOwnerUser.Id, createAccountResult.CreatedId)
        );

        var request = new RemoveUserFromAccountRequest(
            nonOwnerUser.Id,
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
        Assert.DoesNotContain(accountEntity.Users, u => u.Id == nonOwnerUser.Id);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var ownerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new RemoveUserFromAccountRequest(
            Guid.CreateVersion7(),
            createAccountResult.CreatedId
        );
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsAccountNotFound_WhenAccountDoesNotExist()
    {
        var user = await CreateUserAsync();

        var request = new RemoveUserFromAccountRequest(user.Id, Guid.CreateVersion7());
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsUserNotInAccount_WhenUserNotInAccount()
    {
        var ownerUser = await CreateUserAsync();
        var otherUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        var request = new RemoveUserFromAccountRequest(otherUser.Id, createAccountResult.CreatedId);
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
        var ownerUser1 = await CreateUserAsync();
        var ownerUser2 = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser1.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await _accountProcessor.AddUserToAccountAsync(
            new AddUserToAccountRequest(ownerUser2.Id, createAccountResult.CreatedId)
        );

        await CreateOwnerRoleAsync(createAccountResult.CreatedId, ownerUser1.Id, ownerUser2.Id);

        var request = new RemoveUserFromAccountRequest(ownerUser1.Id, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSoleOwnerError_WhenSoleOwnerRemoved()
    {
        var ownerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await CreateOwnerRoleAsync(createAccountResult.CreatedId, ownerUser.Id);

        var request = new RemoveUserFromAccountRequest(ownerUser.Id, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserIsSoleOwnerError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSuccess_WhenOwnerRemovedButOtherOwnerExistsViaGroup()
    {
        var ownerUser1 = await CreateUserAsync();
        var ownerUser2 = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser1.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await _accountProcessor.AddUserToAccountAsync(
            new AddUserToAccountRequest(ownerUser2.Id, createAccountResult.CreatedId)
        );

        await CreateOwnerRoleWithGroupAsync(
            createAccountResult.CreatedId,
            directUserIds: [ownerUser1.Id],
            groupUserIds: [ownerUser2.Id]
        );

        var request = new RemoveUserFromAccountRequest(ownerUser1.Id, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUserFromAccountAsync_ReturnsSoleOwnerError_WhenOwnerViaGroupIsOnlyOwner()
    {
        var ownerUser = await CreateUserAsync();
        var createAccountRequest = new CreateAccountRequest(VALID_ACCOUNT_NAME, ownerUser.Id);
        var createAccountResult = await _accountProcessor.CreateAccountAsync(createAccountRequest);

        await CreateOwnerRoleWithGroupAsync(
            createAccountResult.CreatedId,
            directUserIds: [],
            groupUserIds: [ownerUser.Id]
        );

        var request = new RemoveUserFromAccountRequest(ownerUser.Id, createAccountResult.CreatedId);
        var result = await _accountProcessor.RemoveUserFromAccountAsync(request);

        Assert.Equal(RemoveUserFromAccountResultCode.UserIsSoleOwnerError, result.ResultCode);
    }
}

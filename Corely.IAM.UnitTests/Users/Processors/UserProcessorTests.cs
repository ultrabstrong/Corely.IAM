using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorTests
{
    private const string VALID_USERNAME = "username";
    private const string VALID_EMAIL = "email@x.y";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserProcessor _userProcessor;

    public UserProcessorTests()
    {
        _userProcessor = new UserProcessor(
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<ISecurityProvider>(),
            _serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<ILogger<UserProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = new AccountEntity { Id = Guid.CreateVersion7() };
        var created = await accountRepo.CreateAsync(account);
        return created;
    }

    private async Task<UserEntity> CreateUserInAccountAsync(Guid accountId)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = await accountRepo.GetAsync(a => a.Id == accountId);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = _fixture.Create<string>(),
            Accounts = account != null ? [account] : [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created;
    }

    private async Task<(UserEntity User, AccountEntity Account)> CreateUserAsync()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);
        return (user, account);
    }

    private async Task<RoleEntity> CreateRoleAsync(Guid accountId, params Guid[] userIds)
    {
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

        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Users = users,
            Groups = [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
            Permissions = [],
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return created;
    }

    private async Task CreateOwnerRoleAsync(Guid accountId, params Guid[] userIds)
    {
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
                    await userRepo.UpdateAsync(user);
                }
                users.Add(user);
            }
        }

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = users,
            Groups = [],
            Permissions = [],
        };

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        await roleRepo.CreateAsync(ownerRole);
    }

    private async Task CreateOwnerRoleWithGroupAsync(
        Guid accountId,
        Guid[] directUserIds,
        Guid[] groupUserIds
    )
    {
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
                    await userRepo.UpdateAsync(user);
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
                    await userRepo.UpdateAsync(user);
                }
                groupUsers.Add(user);
            }
        }

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = groupUsers,
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = directUsers,
            Groups = [createdGroup],
            Permissions = [],
        };

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        await roleRepo.CreateAsync(ownerRole);
    }

    [Fact]
    public async Task CreateUserAsync_Fails_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);

        var result = await _userProcessor.CreateUserAsync(request);

        Assert.Equal(CreateUserResultCode.UserExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreateUserResult()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var res = await _userProcessor.CreateUserAsync(request);
        Assert.NotEqual(Guid.Empty, res.CreatedId);
        Assert.Equal(CreateUserResultCode.Success, res.ResultCode);
    }

    [Fact]
    public async Task CreateUser_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _userProcessor.CreateUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task GetUserByUseridAsync_ReturnsNull_WhenUserNotFound()
    {
        var result = await _userProcessor.GetUserAsync(Guid.CreateVersion7());
        Assert.Equal(GetUserResultCode.UserNotFoundError, result.ResultCode);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task GetUserByUseridAsync_ReturnsUser_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var createResult = await _userProcessor.CreateUserAsync(request);

        var result = await _userProcessor.GetUserAsync(createResult.CreatedId);

        Assert.NotNull(result.User);
        Assert.Equal(request.Username, result.User.Username);
        Assert.Equal(request.Email, result.User.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var createUserResult = await _userProcessor.CreateUserAsync(request);

        var updateRequest = new UpdateUserRequest(
            createUserResult.CreatedId,
            "newusername",
            "new@test.com"
        );
        var updateResult = await _userProcessor.UpdateUserAsync(updateRequest);
        var getResult = await _userProcessor.GetUserAsync(createUserResult.CreatedId);

        Assert.Equal(ModifyResultCode.Success, updateResult.ResultCode);
        Assert.Equal("newusername", getResult.User!.Username);
        Assert.Equal("new@test.com", getResult.User.Email);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsKey()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var keyResult = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(
            result.CreatedId
        );

        Assert.Equal(GetAsymmetricKeyResultCode.Success, keyResult.ResultCode);
        Assert.NotNull(keyResult.PublicKey);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsUserNotFound_WhenUserDNE()
    {
        var result = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(
            Guid.CreateVersion7()
        );

        Assert.Equal(GetAsymmetricKeyResultCode.UserNotFoundError, result.ResultCode);
        Assert.Null(result.PublicKey);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsKeyNotFound_WhenSignatureKeyDNE()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var createResult = await _userProcessor.CreateUserAsync(request);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == createResult.CreatedId);
        user?.AsymmetricKeys?.Clear();
        await userRepo.UpdateAsync(user!);

        var result = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(
            createResult.CreatedId
        );

        Assert.Equal(GetAsymmetricKeyResultCode.KeyNotFoundError, result.ResultCode);
        Assert.Null(result.PublicKey);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new AssignRolesToUserRequest([], Guid.CreateVersion7());
        var result = await _userProcessor.AssignRolesToUserAsync(request);
        Assert.Equal(AssignRolesToUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenRolesNotProvided()
    {
        var (user, _) = await CreateUserAsync();
        var request = new AssignRolesToUserRequest([], user.Id);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Succeeds_WhenRolesAssigned()
    {
        var (user, account) = await CreateUserAsync();
        var role = await CreateRoleAsync(account.Id);
        var request = new AssignRolesToUserRequest([role.Id], user.Id);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var userEntity = await userRepo.GetAsync(
            g => g.Id == user.Id,
            include: u => u.Include(u => u.Roles)
        );

        Assert.NotNull(userEntity);
        Assert.NotNull(userEntity.Roles);
        Assert.Contains(userEntity.Roles, r => r.Id == role.Id);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenUserExistsAndNotOwner()
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = new UserEntity
        {
            Username = _fixture.Create<string>(),
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);

        var result = await _userProcessor.DeleteUserAsync(created.Id);

        Assert.Equal(DeleteUserResultCode.Success, result.ResultCode);

        var deletedUser = await userRepo.GetAsync(u => u.Id == created.Id);
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var result = await _userProcessor.DeleteUserAsync(Guid.CreateVersion7());

        Assert.Equal(DeleteUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSoleOwnerError_WhenUserIsSoleOwner()
    {
        var (user, account) = await CreateUserAsync();
        await CreateOwnerRoleAsync(account.Id, user.Id);

        var result = await _userProcessor.DeleteUserAsync(user.Id);

        Assert.Equal(DeleteUserResultCode.UserIsSoleAccountOwnerError, result.ResultCode);
        Assert.Contains("sole owner", result.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenOtherOwnerExistsDirectly()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, user1.Id, user2.Id);

        var result = await _userProcessor.DeleteUserAsync(user1.Id);

        Assert.Equal(DeleteUserResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenOtherOwnerExistsViaGroup()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleWithGroupAsync(
            account.Id,
            directUserIds: [user1.Id],
            groupUserIds: [user2.Id]
        );

        var result = await _userProcessor.DeleteUserAsync(user1.Id);

        Assert.Equal(DeleteUserResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new RemoveRolesFromUserRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );

        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenNonOwnerRoleRemoved()
    {
        var (user, account) = await CreateUserAsync();
        var role = await CreateRoleAsync(account.Id, user.Id);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        user!.Roles = [role!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([role.Id], user.Id);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenOwnerRoleRemovedAndUserIsNotSoleOwner()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, user1.Id, user2.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        user1!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user1);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], user1.Id);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenOwnerRoleRemovedAndUserHasOwnershipViaGroup()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleWithGroupAsync(
            account.Id,
            directUserIds: [user.Id],
            groupUserIds: [user.Id]
        );

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        user!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], user.Id);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Fails_WhenOwnerRoleRemovedAndUserIsSoleOwner()
    {
        var (user, account) = await CreateUserAsync();
        await CreateOwnerRoleAsync(account.Id, user.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        user!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], user.Id);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Equal(0, result.RemovedRoleCount);
        Assert.Contains(ownerRole.Id, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_PartialSuccess_WhenMixedOwnerAndNonOwnerRoles()
    {
        var (user, account) = await CreateUserAsync();
        await CreateOwnerRoleAsync(account.Id, user.Id);
        var regularRole = await CreateRoleAsync(account.Id, user.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == account.Id && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();

        user!.Roles = [ownerRole!, regularRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id, regularRole.Id], user.Id);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
        Assert.Contains(ownerRole.Id, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _userProcessor.RemoveRolesFromUserAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}

using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
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
            _serviceFactory.GetRequiredService<ILogger<UserProcessor>>()
        );
    }

    private async Task<int> CreateAccountAsync()
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = new AccountEntity { Id = _fixture.Create<int>() };
        var created = await accountRepo.CreateAsync(account);
        return created.Id;
    }

    private async Task<int> CreateUserInAccountAsync(int accountId)
    {
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var account = await accountRepo.GetAsync(a => a.Id == accountId);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = new UserEntity
        {
            Id = _fixture.Create<int>(),
            Username = _fixture.Create<string>(),
            Accounts = account != null ? [account] : [],
            Groups = [],
            Roles = [],
        };
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    private async Task<(int UserId, int AccountId)> CreateUserAsync()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);
        return (userId, accountId);
    }

    private async Task<int> CreateRoleAsync(int accountId, params int[] userIds)
    {
        var roleId = _fixture.Create<int>();
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
            Id = roleId,
            Users = users,
            Groups = [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
            Permissions = [],
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return created.Id;
    }

    private async Task CreateOwnerRoleAsync(int accountId, params int[] userIds)
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
            Id = _fixture.Create<int>(),
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
        int accountId,
        int[] directUserIds,
        int[] groupUserIds
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
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = groupUsers,
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
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
        var user = await _userProcessor.GetUserAsync(_fixture.Create<int>());
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByUseridAsync_ReturnsUser_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var user = await _userProcessor.GetUserAsync(result.CreatedId);

        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ReturnsNull_WhenUserNotFound()
    {
        var user = await _userProcessor.GetUserAsync(_fixture.Create<string>());
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ReturnsUser_WhenUserExists()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);

        var user = await _userProcessor.GetUserAsync(request.Username);

        Assert.NotNull(user);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUser()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        await _userProcessor.CreateUserAsync(request);
        var user = await _userProcessor.GetUserAsync(request.Username);
        user!.Disabled = false;

        await _userProcessor.UpdateUserAsync(user);
        var updatedUser = await _userProcessor.GetUserAsync(request.Username);

        Assert.False(updatedUser!.Disabled);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsKey()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(result.CreatedId);

        Assert.NotNull(key);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsNull_WhenUserDNE()
    {
        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(
            _fixture.Create<int>()
        );

        Assert.Null(key);
    }

    [Fact]
    public async Task GetAsymmetricSignatureVerificationKeyAsync_ReturnsNull_WhenSignatureKeyDNE()
    {
        var request = new CreateUserRequest(VALID_USERNAME, VALID_EMAIL);
        var result = await _userProcessor.CreateUserAsync(request);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == result.CreatedId);
        user?.AsymmetricKeys?.Clear();
        await userRepo.UpdateAsync(user!);

        var key = await _userProcessor.GetAsymmetricSignatureVerificationKeyAsync(result.CreatedId);

        Assert.Null(key);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new AssignRolesToUserRequest([], _fixture.Create<int>());
        var result = await _userProcessor.AssignRolesToUserAsync(request);
        Assert.Equal(AssignRolesToUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Fails_WhenRolesNotProvided()
    {
        var (userId, _) = await CreateUserAsync();
        var request = new AssignRolesToUserRequest([], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.InvalidRoleIdsError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToUserAsync_Succeeds_WhenRolesAssigned()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToUserRequest([roleId], userId);

        var result = await _userProcessor.AssignRolesToUserAsync(request);

        Assert.Equal(AssignRolesToUserResultCode.Success, result.ResultCode);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var userEntity = await userRepo.GetAsync(
            g => g.Id == userId,
            include: u => u.Include(u => u.Roles)
        );

        Assert.NotNull(userEntity);
        Assert.NotNull(userEntity.Roles);
        Assert.Contains(userEntity.Roles, r => r.Id == roleId);
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
        var result = await _userProcessor.DeleteUserAsync(_fixture.Create<int>());

        Assert.Equal(DeleteUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSoleOwnerError_WhenUserIsSoleOwner()
    {
        var (userId, accountId) = await CreateUserAsync();
        await CreateOwnerRoleAsync(accountId, userId);

        var result = await _userProcessor.DeleteUserAsync(userId);

        Assert.Equal(DeleteUserResultCode.UserIsSoleAccountOwnerError, result.ResultCode);
        Assert.Contains("sole owner", result.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenOtherOwnerExistsDirectly()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, userId1, userId2);

        var result = await _userProcessor.DeleteUserAsync(userId1);

        Assert.Equal(DeleteUserResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsSuccess_WhenOtherOwnerExistsViaGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleWithGroupAsync(
            accountId,
            directUserIds: [userId1],
            groupUserIds: [userId2]
        );

        var result = await _userProcessor.DeleteUserAsync(userId1);

        Assert.Equal(DeleteUserResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Fails_WhenUserDoesNotExist()
    {
        var request = new RemoveRolesFromUserRequest([1, 2], _fixture.Create<int>());

        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UserNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenNonOwnerRoleRemoved()
    {
        var (userId, accountId) = await CreateUserAsync();
        var roleId = await CreateRoleAsync(accountId, userId);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == userId);
        var role = await roleRepo.GetAsync(r => r.Id == roleId);
        user!.Roles = [role!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([roleId], userId);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenOwnerRoleRemovedAndUserIsNotSoleOwner()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, userId1, userId2);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user1 = await userRepo.GetAsync(u => u.Id == userId1);
        user1!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user1);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], userId1);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Succeeds_WhenOwnerRoleRemovedAndUserHasOwnershipViaGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleWithGroupAsync(
            accountId,
            directUserIds: [userId],
            groupUserIds: [userId]
        );

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == userId);
        user!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], userId);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_Fails_WhenOwnerRoleRemovedAndUserIsSoleOwner()
    {
        var (userId, accountId) = await CreateUserAsync();
        await CreateOwnerRoleAsync(accountId, userId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == userId);
        user!.Roles = [ownerRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id], userId);
        var result = await _userProcessor.RemoveRolesFromUserAsync(request);

        Assert.Equal(RemoveRolesFromUserResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Equal(0, result.RemovedRoleCount);
        Assert.Contains(ownerRole.Id, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task RemoveRolesFromUserAsync_PartialSuccess_WhenMixedOwnerAndNonOwnerRoles()
    {
        var (userId, accountId) = await CreateUserAsync();
        await CreateOwnerRoleAsync(accountId, userId);
        var regularRoleId = await CreateRoleAsync(accountId, userId);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var ownerRole = await roleRepo.GetAsync(r =>
            r.AccountId == accountId && r.Name == RoleConstants.OWNER_ROLE_NAME
        );
        var regularRole = await roleRepo.GetAsync(r => r.Id == regularRoleId);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == userId);
        user!.Roles = [ownerRole!, regularRole!];
        await userRepo.UpdateAsync(user);

        var request = new RemoveRolesFromUserRequest([ownerRole!.Id, regularRoleId], userId);
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

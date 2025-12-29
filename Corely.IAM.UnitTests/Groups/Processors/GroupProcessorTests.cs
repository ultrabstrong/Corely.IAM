using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.UnitTests.ClassData;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Processors;
using Corely.IAM.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorTests
{
    private const string VALID_GROUP_NAME = "groupname";

    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly GroupProcessor _groupProcessor;

    public GroupProcessorTests()
    {
        _groupProcessor = new GroupProcessor(
            _serviceFactory.GetRequiredService<IRepo<GroupEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<GroupProcessor>>()
        );
    }

    private async Task<AccountEntity> CreateAccountAsync()
    {
        var account = new AccountEntity { Id = Guid.CreateVersion7() };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var created = await accountRepo.CreateAsync(account);
        return created;
    }

    private async Task<UserEntity> CreateUserAsync(Guid accountId, params Guid[] groupIds)
    {
        var user = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Groups = groupIds?.Select(g => new GroupEntity { Id = g })?.ToList() ?? [],
            Accounts = [new AccountEntity { Id = accountId }],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);
        return created;
    }

    private async Task<RoleEntity> CreateRoleAsync(Guid accountId, params Guid[] groupIds)
    {
        var role = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Groups = groupIds?.Select(g => new GroupEntity { Id = g })?.ToList() ?? [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return created;
    }

    private async Task<(GroupEntity Group, AccountEntity Account)> CreateGroupAsync()
    {
        var account = await CreateAccountAsync();
        var group = new GroupEntity
        {
            Name = VALID_GROUP_NAME,
            AccountId = account.Id,
            Account = new AccountEntity { Id = account.Id },
        };
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var created = await groupRepo.CreateAsync(group);
        return (created, account);
    }

    private async Task<(GroupEntity Group, AccountEntity Account)> CreateGroupWithUsersAsync(
        params Guid[] userIds
    )
    {
        var account = await CreateAccountAsync();
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

        var group = new GroupEntity
        {
            Name = VALID_GROUP_NAME,
            AccountId = account.Id,
            Account = new AccountEntity { Id = account.Id },
            Users = users,
        };
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var created = await groupRepo.CreateAsync(group);
        return (created, account);
    }

    [Fact]
    public async Task CreateGroupAsync_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreateGroupRequest(VALID_GROUP_NAME, Guid.CreateVersion7());

        var result = await _groupProcessor.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateGroupAsync_Fails_WhenGroupExists()
    {
        var account = await CreateAccountAsync();
        var request = new CreateGroupRequest(VALID_GROUP_NAME, account.Id);
        await _groupProcessor.CreateGroupAsync(request);

        var result = await _groupProcessor.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.GroupExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateGroupAsync_ReturnsCreateGroupResult()
    {
        var account = await CreateAccountAsync();
        var request = new CreateGroupRequest(VALID_GROUP_NAME, account.Id);

        var result = await _groupProcessor.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.Success, result.ResultCode);

        // Verify group is linked to account id
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(
            g => g.Id == result.CreatedId,
            include: q => q.Include(g => g.Account)
        );
        Assert.NotNull(groupEntity);
        //Assert.NotNull(groupEntity.Account); // Account not available for memory mock repo
        Assert.Equal(account.Id, groupEntity.AccountId);
    }

    [Fact]
    public async Task CreateGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _groupProcessor.CreateGroupAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Theory, ClassData(typeof(NullEmptyAndWhitespace))]
    public async Task CreateGroupAsync_Throws_WithInvalidGroupName(string groupName)
    {
        var account = await CreateAccountAsync();
        var request = new CreateGroupRequest(groupName, account.Id);

        var ex = await Record.ExceptionAsync(() => _groupProcessor.CreateGroupAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new AddUsersToGroupRequest([], Guid.CreateVersion7());
        var result = await _groupProcessor.AddUsersToGroupAsync(request);
        Assert.Equal(AddUsersToGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenUsersNotProvided()
    {
        var (group, _) = await CreateGroupAsync();
        var request = new AddUsersToGroupRequest([], group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.InvalidUserIdsError, result.ResultCode);
        Assert.Equal(
            "All user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Succeeds_WhenUsersAdded()
    {
        var (group, account) = await CreateGroupAsync();
        var user = await CreateUserAsync(account.Id);
        var request = new AddUsersToGroupRequest([user.Id], group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(
            g => g.Id == group.Id,
            include: q => q.Include(g => g.Users)
        );

        Assert.NotNull(groupEntity);
        Assert.NotNull(groupEntity.Users);
        Assert.Contains(groupEntity.Users, u => u.Id == user.Id);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersExistInGroup()
    {
        var (group, account) = await CreateGroupAsync();
        var existingUser = await CreateUserAsync(account.Id, group.Id);
        var newUser = await CreateUserAsync(account.Id);
        var request = new AddUsersToGroupRequest([existingUser.Id, newUser.Id], group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(1, result.AddedUserCount);
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(existingUser.Id, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersDoNotExist()
    {
        var (group, account) = await CreateGroupAsync();
        var user = await CreateUserAsync(account.Id);
        var request = new AddUsersToGroupRequest([user.Id, Guid.Empty], group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(Guid.Empty, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersDoNotHaveGroupAccount()
    {
        var (group, account) = await CreateGroupAsync();
        var userSameAccount = await CreateUserAsync(account.Id);
        var userDifferentAccount = await CreateUserAsync(Guid.CreateVersion7());
        var request = new AddUsersToGroupRequest(
            [userSameAccount.Id, userDifferentAccount.Id],
            group.Id
        );

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(1, result.AddedUserCount);
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(userDifferentAccount.Id, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenAllUsersExistInGroup()
    {
        var (group, account) = await CreateGroupAsync();
        var userIds = new List<Guid>()
        {
            (await CreateUserAsync(account.Id, group.Id)).Id,
            (await CreateUserAsync(account.Id, group.Id)).Id,
        };
        var request = new AddUsersToGroupRequest(userIds, group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.InvalidUserIdsError, result.ResultCode);
        Assert.Equal(
            "All user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(0, result.AddedUserCount);
        Assert.Equal(userIds, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenAllUsersDoNotExist()
    {
        var (group, _) = await CreateGroupAsync();
        var userIds = _fixture.CreateMany<Guid>().ToList();
        var request = new AddUsersToGroupRequest(userIds, group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.InvalidUserIdsError, result.ResultCode);
        Assert.Equal(
            "All user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(0, result.AddedUserCount);
        Assert.Equal(userIds, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenAllUsersDoNotHaveGroupAccount()
    {
        var (group, _) = await CreateGroupAsync();
        var userIds = new List<Guid>()
        {
            (await CreateUserAsync(Guid.CreateVersion7())).Id,
            (await CreateUserAsync(Guid.CreateVersion7())).Id,
        };
        var request = new AddUsersToGroupRequest(userIds, group.Id);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.InvalidUserIdsError, result.ResultCode);
        Assert.Equal(
            "All user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(0, result.AddedUserCount);
        Assert.Equal(userIds, result.InvalidUserIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new AssignRolesToGroupRequest([], Guid.CreateVersion7());
        var result = await _groupProcessor.AssignRolesToGroupAsync(request);
        Assert.Equal(AssignRolesToGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenRolesNotProvided()
    {
        var (group, _) = await CreateGroupAsync();
        var request = new AssignRolesToGroupRequest([], group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Succeeds_WhenRolesAssigned()
    {
        var (group, account) = await CreateGroupAsync();
        var role = await CreateRoleAsync(account.Id);
        var request = new AssignRolesToGroupRequest([role.Id], group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(
            g => g.Id == group.Id,
            include: q => q.Include(g => g.Roles)
        );

        Assert.NotNull(groupEntity);
        Assert.NotNull(groupEntity.Roles);
        Assert.Contains(groupEntity.Roles, r => r.Id == role.Id);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesExistForGroup()
    {
        var (group, account) = await CreateGroupAsync();
        var existingRole = await CreateRoleAsync(account.Id, group.Id);
        var newRole = await CreateRoleAsync(account.Id, Guid.CreateVersion7());
        var request = new AssignRolesToGroupRequest([existingRole.Id, newRole.Id], group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(existingRole.Id, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesDoNotExist()
    {
        var (group, account) = await CreateGroupAsync();
        var role = await CreateRoleAsync(account.Id);
        var invalidRoleId = Guid.CreateVersion7();
        var request = new AssignRolesToGroupRequest([role.Id, invalidRoleId], group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(invalidRoleId, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesBelongToDifferentAccount()
    {
        var (group, account) = await CreateGroupAsync();
        var roleSameAccount = await CreateRoleAsync(account.Id);
        var roleDifferentAccount = await CreateRoleAsync(Guid.CreateVersion7());
        var request = new AssignRolesToGroupRequest(
            [roleSameAccount.Id, roleDifferentAccount.Id],
            group.Id
        );

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(roleDifferentAccount.Id, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenAllRolesExistForGroup()
    {
        var (group, account) = await CreateGroupAsync();
        var roleIds = new List<Guid>
        {
            (await CreateRoleAsync(account.Id, group.Id)).Id,
            (await CreateRoleAsync(account.Id, group.Id)).Id,
        };
        var request = new AssignRolesToGroupRequest(roleIds, group.Id);
        await _groupProcessor.AssignRolesToGroupAsync(request);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenAllRolesDoNotExist()
    {
        var (group, _) = await CreateGroupAsync();
        var roleIds = _fixture.CreateMany<Guid>().ToList();
        var request = new AssignRolesToGroupRequest(roleIds, group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(0, result.AddedRoleCount);
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenAllRolesBelongToDifferentAccount()
    {
        var (group, _) = await CreateGroupAsync();
        var roleIds = new List<Guid>()
        {
            (await CreateRoleAsync(Guid.CreateVersion7())).Id,
            (await CreateRoleAsync(Guid.CreateVersion7())).Id,
        };
        var request = new AssignRolesToGroupRequest(roleIds, group.Id);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.InvalidRoleIdsError, result.ResultCode);
        Assert.Equal(
            "All role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(0, result.AddedRoleCount);
        Assert.Equal(roleIds, result.InvalidRoleIds);
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsSuccess_WhenGroupExists()
    {
        var (group, _) = await CreateGroupAsync();

        var result = await _groupProcessor.DeleteGroupAsync(group.Id);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(g => g.Id == group.Id);
        Assert.Null(groupEntity);
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        var result = await _groupProcessor.DeleteGroupAsync(Guid.CreateVersion7());

        Assert.Equal(DeleteGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new RemoveUsersFromGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );

        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenUsersRemoved()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserAsync(account.Id);
        var (group, _) = await CreateGroupWithUsersAsync(user.Id);

        var request = new RemoveUsersFromGroupRequest([user.Id], group.Id);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedUserCount);
        Assert.Empty(result.InvalidUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenNoUsersInGroup()
    {
        var (group, _) = await CreateGroupAsync();

        var invalidUserId = Guid.CreateVersion7();
        var request = new RemoveUsersFromGroupRequest([invalidUserId], group.Id);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(0, result.RemovedUserCount);
        Assert.Contains(invalidUserId, result.InvalidUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_PartiallySucceeds_WhenSomeUsersNotInGroup()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserAsync(account.Id);
        var (group, _) = await CreateGroupWithUsersAsync(user.Id);

        var invalidUserId = Guid.CreateVersion7();
        var request = new RemoveUsersFromGroupRequest([user.Id, invalidUserId], group.Id);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedUserCount);
        Assert.Contains(invalidUserId, result.InvalidUserIds);
        Assert.DoesNotContain(user.Id, result.InvalidUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _groupProcessor.RemoveUsersFromGroupAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    private async Task<(
        Guid GroupId,
        Guid AccountId,
        List<Guid> UserIds
    )> CreateGroupWithOwnerRoleAndUsersAsync(int userCount, bool assignOwnerRoleToGroup = true)
    {
        var account = await CreateAccountAsync();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        // Create users in the account
        var userIds = new List<Guid>();
        var users = new List<UserEntity>();
        for (int i = 0; i < userCount; i++)
        {
            var user = new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = _fixture.Create<string>(),
                Accounts = account != null ? [account] : [],
                Groups = [],
                Roles = [],
            };
            var createdUser = await userRepo.CreateAsync(user);
            userIds.Add(createdUser.Id);
            users.Add(createdUser);
        }

        // Create group with users
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = VALID_GROUP_NAME,
            AccountId = account!.Id,
            Users = users,
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        // Create owner role and optionally assign to group
        if (assignOwnerRoleToGroup)
        {
            var ownerRole = new RoleEntity
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                Name = RoleConstants.OWNER_ROLE_NAME,
                IsSystemDefined = true,
                Users = [],
                Groups = [createdGroup],
                Permissions = [],
            };
            await roleRepo.CreateAsync(ownerRole);

            // Update group with role reference
            createdGroup.Roles = [ownerRole];
            await groupRepo.UpdateAsync(createdGroup);
        }

        return (createdGroup.Id, account.Id, userIds);
    }

    private async Task AssignDirectOwnerRoleToUserAsync(Guid userId, Guid accountId)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user = await userRepo.GetAsync(u => u.Id == userId);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [user!],
            Groups = [],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenGroupHasNoOwnerRole()
    {
        // Create group WITHOUT owner role
        var (groupId, _, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2,
            assignOwnerRoleToGroup: false
        );

        var request = new RemoveUsersFromGroupRequest(userIds, groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedUserCount);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenGroupHasOwnerRoleAndSomeUsersRemain()
    {
        var (groupId, _, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(userCount: 3);

        // Remove only 2 of 3 users - one remains to hold the owner role
        var request = new RemoveUsersFromGroupRequest([userIds[0], userIds[1]], groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedUserCount);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenAllUsersRemovedButOneHasDirectOwnership()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2
        );

        // Give one user direct owner role (outside the group)
        await AssignDirectOwnerRoleToUserAsync(userIds[0], accountId);

        // Remove all users - should succeed because userId[0] has direct ownership
        var request = new RemoveUsersFromGroupRequest(userIds, groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(2, result.RemovedUserCount);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Fails_WhenAllUsersRemovedAndNoneHaveOwnershipElsewhere()
    {
        var (groupId, _, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(userCount: 2);

        // Don't give any user ownership elsewhere - all ownership is via this group

        // Try to remove all users - should fail
        var request = new RemoveUsersFromGroupRequest(userIds, groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Equal(0, result.RemovedUserCount);
        Assert.Equal(userIds.Count, result.SoleOwnerUserIds.Count);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Fails_WhenSingleUserRemovedFromOwnerGroupWithNoOtherOwnership()
    {
        var (groupId, _, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(userCount: 1);

        // Single user, only ownership via group - should fail
        var request = new RemoveUsersFromGroupRequest(userIds, groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.UserIsSoleOwnerError, result.ResultCode);
        Assert.Equal(0, result.RemovedUserCount);
        Assert.Single(result.SoleOwnerUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenSingleUserRemovedFromOwnerGroupWithDirectOwnership()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

        // Give the single user direct owner role
        await AssignDirectOwnerRoleToUserAsync(userIds[0], accountId);

        // Remove the user - should succeed because they have direct ownership
        var request = new RemoveUsersFromGroupRequest(userIds, groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedUserCount);
    }

    [Fact]
    public async Task DeleteGroupAsync_Succeeds_WhenGroupHasNoOwnerRole()
    {
        var (groupId, _, _) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2,
            assignOwnerRoleToGroup: false
        );

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteGroupAsync_Succeeds_WhenGroupHasOwnerRoleButNoUsers()
    {
        // Create a group with owner role but no users
        var account = await CreateAccountAsync();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = VALID_GROUP_NAME,
            AccountId = account.Id,
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);
        createdGroup.Roles = [ownerRole];
        await groupRepo.UpdateAsync(createdGroup);

        var result = await _groupProcessor.DeleteGroupAsync(createdGroup.Id);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteGroupAsync_Succeeds_WhenGroupHasOwnerRoleAndUserHasDirectOwnership()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

        // Give the user direct owner role
        await AssignDirectOwnerRoleToUserAsync(userIds[0], accountId);

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DeleteGroupAsync_Fails_WhenGroupHasOwnerRoleAndNoUserHasOwnershipElsewhere()
    {
        var (groupId, _, _) = await CreateGroupWithOwnerRoleAndUsersAsync(userCount: 2);

        // Don't give any user ownership elsewhere

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.GroupHasSoleOwnersError, result.ResultCode);
        Assert.Contains("owner role", result.Message);
    }

    [Fact]
    public async Task DeleteGroupAsync_Fails_WhenSingleUserInOwnerGroupWithNoOtherOwnership()
    {
        var (groupId, _, _) = await CreateGroupWithOwnerRoleAndUsersAsync(userCount: 1);

        // Single user, only ownership via group - should fail
        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.GroupHasSoleOwnersError, result.ResultCode);
    }

    [Fact]
    public async Task DeleteGroupAsync_Succeeds_WhenMultipleUsersAndOneHasOwnershipElsewhere()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 3
        );

        // Give one user direct owner role
        await AssignDirectOwnerRoleToUserAsync(userIds[0], accountId);

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new RemoveRolesFromGroupRequest(
            [Guid.CreateVersion7(), Guid.CreateVersion7()],
            Guid.CreateVersion7()
        );

        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Fails_WhenRolesNotAssignedToGroup()
    {
        var (group, _) = await CreateGroupAsync();

        var request = new RemoveRolesFromGroupRequest([Guid.CreateVersion7()], group.Id);
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.InvalidRoleIdsError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Succeeds_WhenNonOwnerRoleRemoved()
    {
        var (group, account) = await CreateGroupAsync();
        var role = await CreateRoleAsync(account.Id, group.Id);

        // Assign role to group
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        group!.Roles = [role];
        await groupRepo.UpdateAsync(group);

        var request = new RemoveRolesFromGroupRequest([role.Id], group.Id);
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Succeeds_WhenOwnerRoleRemovedFromGroupWithNoUsers()
    {
        // Create group with owner role but no users
        var account = await CreateAccountAsync();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = VALID_GROUP_NAME,
            AccountId = account.Id,
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup],
            Permissions = [],
        };
        var createdRole = await roleRepo.CreateAsync(ownerRole);
        createdGroup.Roles = [createdRole];
        await groupRepo.UpdateAsync(createdGroup);

        var request = new RemoveRolesFromGroupRequest([createdRole.Id], createdGroup.Id);
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Succeeds_WhenOwnerRoleRemovedAndUserHasDirectOwnership()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

        // Give the user direct owner role
        await AssignDirectOwnerRoleToUserAsync(userIds[0], accountId);

        // Get the owner role assigned to the group
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = await groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Roles)
        );
        var ownerRoleId = group!.Roles!.First(r => r.Name == RoleConstants.OWNER_ROLE_NAME).Id;

        var request = new RemoveRolesFromGroupRequest([ownerRoleId], groupId);
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(RemoveRolesFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Fails_WhenOwnerRoleRemovedAndNoUserHasOwnershipElsewhere()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2
        );

        // Don't give any user ownership elsewhere

        // Get the owner role assigned to the group
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = await groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Roles)
        );
        var ownerRoleId = group!.Roles!.First(r => r.Name == RoleConstants.OWNER_ROLE_NAME).Id;

        var request = new RemoveRolesFromGroupRequest([ownerRoleId], groupId);
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        Assert.Equal(
            RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
            result.ResultCode
        );
        Assert.Equal(0, result.RemovedRoleCount);
        Assert.Contains(ownerRoleId, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_PartialSuccess_WhenMixedOwnerAndNonOwnerRoles()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

        // Don't give user ownership elsewhere - owner role removal should be blocked

        // Create and assign a regular role to the group
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var group = await groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Roles).Include(g => g.Users)
        );

        var regularRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            Name = "RegularRole",
            IsSystemDefined = false,
            Users = [],
            Groups = [group!],
            Permissions = [],
        };
        var createdRegularRole = await roleRepo.CreateAsync(regularRole);
        group!.Roles!.Add(createdRegularRole);
        await groupRepo.UpdateAsync(group);

        var ownerRoleId = group.Roles.First(r => r.Name == RoleConstants.OWNER_ROLE_NAME).Id;

        var request = new RemoveRolesFromGroupRequest(
            [ownerRoleId, createdRegularRole.Id],
            groupId
        );
        var result = await _groupProcessor.RemoveRolesFromGroupAsync(request);

        // Should remove regular role but block owner role
        Assert.Equal(RemoveRolesFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedRoleCount);
        Assert.Contains(ownerRoleId, result.BlockedOwnerRoleIds);
    }

    [Fact]
    public async Task RemoveRolesFromGroupAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() =>
            _groupProcessor.RemoveRolesFromGroupAsync(null!)
        );

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}

using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
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

    private async Task<int> CreateAccountAsync()
    {
        var accountId = _fixture.Create<int>();
        var account = new AccountEntity { Id = accountId };
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();
        var created = await accountRepo.CreateAsync(account);
        return created.Id;
    }

    private async Task<int> CreateUserAsync(int accountId, params int[] groupIds)
    {
        var userId = _fixture.Create<int>();
        var user = new UserEntity
        {
            Id = userId,
            Groups = groupIds?.Select(g => new GroupEntity { Id = g })?.ToList() ?? [],
            Accounts = [new AccountEntity { Id = accountId }],
        };
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var created = await userRepo.CreateAsync(user);
        return created.Id;
    }

    private async Task<int> CreateRoleAsync(int accountId, params int[] groupIds)
    {
        var roleId = _fixture.Create<int>();
        var role = new RoleEntity
        {
            Id = roleId,
            Groups = groupIds?.Select(g => new GroupEntity { Id = g })?.ToList() ?? [],
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
        };
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var created = await roleRepo.CreateAsync(role);
        return created.Id;
    }

    private async Task<(int GroupId, int AccountId)> CreateGroupAsync()
    {
        var accountId = await CreateAccountAsync();
        var group = new GroupEntity
        {
            Name = VALID_GROUP_NAME,
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
        };
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var created = await groupRepo.CreateAsync(group);
        return (created.Id, accountId);
    }

    private async Task<(int GroupId, int AccountId)> CreateGroupWithUsersAsync(params int[] userIds)
    {
        var accountId = await CreateAccountAsync();
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
            AccountId = accountId,
            Account = new AccountEntity { Id = accountId },
            Users = users,
        };
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var created = await groupRepo.CreateAsync(group);
        return (created.Id, accountId);
    }

    [Fact]
    public async Task CreateGroupAsync_Fails_WhenAccountDoesNotExist()
    {
        var request = new CreateGroupRequest(VALID_GROUP_NAME, _fixture.Create<int>());

        var result = await _groupProcessor.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.AccountNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task CreateGroupAsync_Fails_WhenGroupExists()
    {
        var request = new CreateGroupRequest(VALID_GROUP_NAME, await CreateAccountAsync());
        await _groupProcessor.CreateGroupAsync(request);

        var result = await _groupProcessor.CreateGroupAsync(request);

        Assert.Equal(CreateGroupResultCode.GroupExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateGroupAsync_ReturnsCreateGroupResult()
    {
        var accountId = await CreateAccountAsync();
        var request = new CreateGroupRequest(VALID_GROUP_NAME, accountId);

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
        Assert.Equal(accountId, groupEntity.AccountId);
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
        var request = new CreateGroupRequest(groupName, await CreateAccountAsync());

        var ex = await Record.ExceptionAsync(() => _groupProcessor.CreateGroupAsync(request));

        Assert.NotNull(ex);
        Assert.IsType<ValidationException>(ex);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new AddUsersToGroupRequest([], _fixture.Create<int>());
        var result = await _groupProcessor.AddUsersToGroupAsync(request);
        Assert.Equal(AddUsersToGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenUsersNotProvided()
    {
        var (groupId, _) = await CreateGroupAsync();
        var request = new AddUsersToGroupRequest([], groupId);

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
        var (groupId, accountId) = await CreateGroupAsync();
        var userId = await CreateUserAsync(accountId);
        var request = new AddUsersToGroupRequest([userId], groupId);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Users)
        );

        Assert.NotNull(groupEntity);
        Assert.NotNull(groupEntity.Users);
        Assert.Contains(groupEntity.Users, u => u.Id == userId);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersExistInGroup()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var existingUserId = await CreateUserAsync(accountId, groupId);
        var newUserId = await CreateUserAsync(accountId);
        var request = new AddUsersToGroupRequest([existingUserId, newUserId], groupId);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(1, result.AddedUserCount);
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(existingUserId, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersDoNotExist()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var userId = await CreateUserAsync(accountId);
        var request = new AddUsersToGroupRequest([userId, -1], groupId);

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(-1, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_PartiallySucceeds_WhenSomeUsersDoNotHaveGroupAccount()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var userIdSameAccount = await CreateUserAsync(accountId);
        var userIdDifferentAccount = await CreateUserAsync(accountId + 1);
        var request = new AddUsersToGroupRequest(
            [userIdSameAccount, userIdDifferentAccount],
            groupId
        );

        var result = await _groupProcessor.AddUsersToGroupAsync(request);

        Assert.Equal(AddUsersToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some user ids are invalid (not found, from different account, or already exist in group)",
            result.Message
        );
        Assert.Equal(1, result.AddedUserCount);
        Assert.NotEmpty(result.InvalidUserIds);
        Assert.Contains(userIdDifferentAccount, result.InvalidUserIds);
    }

    [Fact]
    public async Task AddUsersToGroupAsync_Fails_WhenAllUsersExistInGroup()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var userIds = new List<int>()
        {
            await CreateUserAsync(accountId, groupId),
            await CreateUserAsync(accountId, groupId),
        };
        var request = new AddUsersToGroupRequest(userIds, groupId);

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
        var (groupId, _) = await CreateGroupAsync();
        var userIds = _fixture.CreateMany<int>().ToList();
        var request = new AddUsersToGroupRequest(userIds, groupId);

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
        var (groupId, accountId) = await CreateGroupAsync();
        var userIds = new List<int>()
        {
            await CreateUserAsync(accountId + 1),
            await CreateUserAsync(accountId + 2),
        };
        var request = new AddUsersToGroupRequest(userIds, groupId);

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
        var request = new AssignRolesToGroupRequest([], _fixture.Create<int>());
        var result = await _groupProcessor.AssignRolesToGroupAsync(request);
        Assert.Equal(AssignRolesToGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenRolesNotProvided()
    {
        var (groupId, _) = await CreateGroupAsync();
        var request = new AssignRolesToGroupRequest([], groupId);

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
        var (groupId, accountId) = await CreateGroupAsync();
        var roleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToGroupRequest([roleId], groupId);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(
            g => g.Id == groupId,
            include: q => q.Include(g => g.Roles)
        );

        Assert.NotNull(groupEntity);
        Assert.NotNull(groupEntity.Roles);
        Assert.Contains(groupEntity.Roles, r => r.Id == roleId);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesExistForGroup()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var existingRoleId = await CreateRoleAsync(accountId, groupId);
        var newRoleId = await CreateRoleAsync(accountId, groupId + 1);
        var request = new AssignRolesToGroupRequest([existingRoleId, newRoleId], groupId);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(existingRoleId, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesDoNotExist()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var roleId = await CreateRoleAsync(accountId);
        var request = new AssignRolesToGroupRequest([roleId, -1], groupId);

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(-1, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_PartiallySucceeds_WhenSomeRolesBelongToDifferentAccount()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var roleIdSameAccount = await CreateRoleAsync(accountId);
        var roleIdDifferentAccount = await CreateRoleAsync(accountId + 1);
        var request = new AssignRolesToGroupRequest(
            [roleIdSameAccount, roleIdDifferentAccount],
            groupId
        );

        var result = await _groupProcessor.AssignRolesToGroupAsync(request);

        Assert.Equal(AssignRolesToGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(
            "Some role ids are invalid (not found, from different account, or already assigned to group)",
            result.Message
        );
        Assert.Equal(1, result.AddedRoleCount);
        Assert.NotEmpty(result.InvalidRoleIds);
        Assert.Contains(roleIdDifferentAccount, result.InvalidRoleIds);
    }

    [Fact]
    public async Task AssignRolesToGroupAsync_Fails_WhenAllRolesExistForGroup()
    {
        var (groupId, accountId) = await CreateGroupAsync();
        var roleIds = new List<int>
        {
            await CreateRoleAsync(accountId, groupId),
            await CreateRoleAsync(accountId, groupId),
        };
        var request = new AssignRolesToGroupRequest(roleIds, groupId);
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
        var (groupId, _) = await CreateGroupAsync();
        var roleIds = _fixture.CreateMany<int>().ToList();
        var request = new AssignRolesToGroupRequest(roleIds, groupId);

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
        var (groupId, accountId) = await CreateGroupAsync();
        var roleIds = new List<int>()
        {
            await CreateRoleAsync(accountId + 1),
            await CreateRoleAsync(accountId + 2),
        };
        var request = new AssignRolesToGroupRequest(roleIds, groupId);

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
        var (groupId, _) = await CreateGroupAsync();

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.Success, result.ResultCode);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var groupEntity = await groupRepo.GetAsync(g => g.Id == groupId);
        Assert.Null(groupEntity);
    }

    [Fact]
    public async Task DeleteGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        var result = await _groupProcessor.DeleteGroupAsync(_fixture.Create<int>());

        Assert.Equal(DeleteGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Fails_WhenGroupDoesNotExist()
    {
        var request = new RemoveUsersFromGroupRequest([1, 2], _fixture.Create<int>());

        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.GroupNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenUsersRemoved()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserAsync(accountId);
        var (groupId, _) = await CreateGroupWithUsersAsync(userId);

        var request = new RemoveUsersFromGroupRequest([userId], groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.Success, result.ResultCode);
        Assert.Equal(1, result.RemovedUserCount);
        Assert.Empty(result.InvalidUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_Succeeds_WhenNoUsersInGroup()
    {
        var (groupId, _) = await CreateGroupAsync();

        var request = new RemoveUsersFromGroupRequest([9999], groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(0, result.RemovedUserCount);
        Assert.Contains(9999, result.InvalidUserIds);
    }

    [Fact]
    public async Task RemoveUsersFromGroupAsync_PartiallySucceeds_WhenSomeUsersNotInGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserAsync(accountId);
        var (groupId, _) = await CreateGroupWithUsersAsync(userId);

        var request = new RemoveUsersFromGroupRequest([userId, 9999], groupId);
        var result = await _groupProcessor.RemoveUsersFromGroupAsync(request);

        Assert.Equal(RemoveUsersFromGroupResultCode.PartialSuccess, result.ResultCode);
        Assert.Equal(1, result.RemovedUserCount);
        Assert.Contains(9999, result.InvalidUserIds);
        Assert.DoesNotContain(userId, result.InvalidUserIds);
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

    #region RemoveUsersFromGroupAsync Ownership Tests

    private async Task<(
        int GroupId,
        int AccountId,
        List<int> UserIds
    )> CreateGroupWithOwnerRoleAndUsersAsync(int userCount, bool assignOwnerRoleToGroup = true)
    {
        var accountId = await CreateAccountAsync();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var accountRepo = _serviceFactory.GetRequiredService<IRepo<AccountEntity>>();

        var account = await accountRepo.GetAsync(a => a.Id == accountId);

        // Create users in the account
        var userIds = new List<int>();
        var users = new List<UserEntity>();
        for (int i = 0; i < userCount; i++)
        {
            var user = new UserEntity
            {
                Id = _fixture.Create<int>(),
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
            Id = _fixture.Create<int>(),
            Name = VALID_GROUP_NAME,
            AccountId = accountId,
            Users = users,
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        // Create owner role and optionally assign to group
        if (assignOwnerRoleToGroup)
        {
            var ownerRole = new RoleEntity
            {
                Id = _fixture.Create<int>(),
                AccountId = accountId,
                Name = Corely.IAM.Roles.Constants.RoleConstants.OWNER_ROLE_NAME,
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

        return (createdGroup.Id, accountId, userIds);
    }

    private async Task AssignDirectOwnerRoleToUserAsync(int userId, int accountId)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user = await userRepo.GetAsync(u => u.Id == userId);

        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = Corely.IAM.Roles.Constants.RoleConstants.OWNER_ROLE_NAME,
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
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
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
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 3
        );

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
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2
        );

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
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

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

    #endregion

    #region DeleteGroupAsync Ownership Tests

    [Fact]
    public async Task DeleteGroupAsync_Succeeds_WhenGroupHasNoOwnerRole()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
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
        var accountId = await CreateAccountAsync();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            Name = VALID_GROUP_NAME,
            AccountId = accountId,
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = Corely.IAM.Roles.Constants.RoleConstants.OWNER_ROLE_NAME,
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
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 2
        );

        // Don't give any user ownership elsewhere

        var result = await _groupProcessor.DeleteGroupAsync(groupId);

        Assert.Equal(DeleteGroupResultCode.GroupHasSoleOwnersError, result.ResultCode);
        Assert.Contains("owner role", result.Message);
    }

    [Fact]
    public async Task DeleteGroupAsync_Fails_WhenSingleUserInOwnerGroupWithNoOtherOwnership()
    {
        var (groupId, accountId, userIds) = await CreateGroupWithOwnerRoleAndUsersAsync(
            userCount: 1
        );

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

    #endregion
}

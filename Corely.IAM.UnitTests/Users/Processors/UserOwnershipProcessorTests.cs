using AutoFixture;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserOwnershipProcessorTests
{
    private readonly Fixture _fixture = new();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserOwnershipProcessor _userOwnershipProcessor;

    public UserOwnershipProcessorTests()
    {
        _userOwnershipProcessor = new UserOwnershipProcessor(
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<ILogger<UserOwnershipProcessor>>()
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

    private async Task CreateOwnerRoleAsync(int accountId, int[] directUserIds, int[] groupUserIds)
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        // Get direct users
        var directUsers = new List<UserEntity>();
        foreach (var userId in directUserIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                directUsers.Add(user);
            }
        }

        // Get/create group users and group
        var groups = new List<GroupEntity>();
        if (groupUserIds.Length > 0)
        {
            var groupUsers = new List<UserEntity>();
            foreach (var userId in groupUserIds)
            {
                var user = await userRepo.GetAsync(u => u.Id == userId);
                if (user != null)
                {
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
            groups.Add(createdGroup);
        }

        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = directUsers,
            Groups = groups,
            Permissions = [],
        };

        await roleRepo.CreateAsync(ownerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenUserDoesNotHaveOwnerRole()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);
        // Don't create any owner role

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenUserHasOwnerRoleButNotInAccount()
    {
        var accountId = await CreateAccountAsync();
        var otherAccountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(otherAccountId); // User in different account

        // Create owner role for the account, but user is in different account
        await CreateOwnerRoleAsync(accountId, directUserIds: [], groupUserIds: []);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsTrue_WhenUserIsSoleOwnerDirectly()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, directUserIds: [userId], groupUserIds: []);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId, accountId);

        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsTrue_WhenUserIsSoleOwnerViaGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, directUserIds: [], groupUserIds: [userId]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId, accountId);

        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenOtherOwnerExistsDirectly()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, directUserIds: [userId1, userId2], groupUserIds: []);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId1, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenOtherOwnerExistsViaGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, directUserIds: [userId1], groupUserIds: [userId2]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId1, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenUserHasOwnerViaGroupAndOtherOwnerExistsDirectly()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        await CreateOwnerRoleAsync(accountId, directUserIds: [userId2], groupUserIds: [userId1]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId1, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsTrue_WhenOtherOwnerExistsButNotInAccount()
    {
        var accountId = await CreateAccountAsync();
        var otherAccountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(otherAccountId); // User2 in different account

        // Both users have owner role, but userId2 is not in this account
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user1 = await userRepo.GetAsync(u => u.Id == userId1);
        var user2 = await userRepo.GetAsync(u => u.Id == userId2);

        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [user1!, user2!], // Both have role, but user2 not in account
            Groups = [],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId1, accountId);

        // userId1 should be sole owner because userId2 is not in the account
        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccountAsync_ReturnsFalse_WhenNoOwnerRoleExistsForAccount()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        // Create a non-owner role
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == userId);

        var regularRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "RegularRole",
            IsSystemDefined = false,
            Users = [user!],
            Groups = [],
            Permissions = [],
        };
        await roleRepo.CreateAsync(regularRole);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(userId, accountId);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroupAsync_ReturnsTrue_WhenUserHasDirectOwnership()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        // Create owner role with direct user assignment
        await CreateOwnerRoleAsync(accountId, directUserIds: [userId], groupUserIds: []);

        // Create a group (the one we're "excluding")
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            userId,
            accountId,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroupAsync_ReturnsTrue_WhenUserHasOwnershipViaOtherGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        // Create two groups
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user = await userRepo.GetAsync(u => u.Id == userId);

        var group1 = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "Group1",
            Users = [user!],
            Roles = [],
        };
        var createdGroup1 = await groupRepo.CreateAsync(group1);

        var group2 = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "Group2",
            Users = [user!],
            Roles = [],
        };
        var createdGroup2 = await groupRepo.CreateAsync(group2);

        // Create owner role assigned to group2 (not group1)
        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup2],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        // Check if user has ownership outside of group1 (they do, via group2)
        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            userId,
            accountId,
            createdGroup1.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroupAsync_ReturnsFalse_WhenOwnershipOnlyViaExcludedGroup()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user = await userRepo.GetAsync(u => u.Id == userId);

        // Create a group with the user
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = [user!],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        // Create owner role assigned only to this group
        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        // Check if user has ownership outside of this group (they don't)
        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            userId,
            accountId,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroupAsync_ReturnsFalse_WhenUserHasNoOwnerRole()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        // Create a group but no owner role
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            userId,
            accountId,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroupAsync_ReturnsTrue_WhenUserHasBothDirectAndGroupOwnership()
    {
        var accountId = await CreateAccountAsync();
        var userId = await CreateUserInAccountAsync(accountId);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user = await userRepo.GetAsync(u => u.Id == userId);

        // Create a group with the user
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = [user!],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        // Create owner role with both direct assignment AND group assignment
        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [user!], // Direct assignment
            Groups = [createdGroup], // Also via group
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        // Even though user is in the excluded group, they have direct ownership
        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            userId,
            accountId,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroupAsync_ReturnsTrue_WhenOneUserHasDirectOwnership()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        // Create owner role with direct assignment for userId1 only
        await CreateOwnerRoleAsync(accountId, directUserIds: [userId1], groupUserIds: []);

        // Create a group (the one we're "excluding")
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [userId1, userId2],
            accountId,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroupAsync_ReturnsFalse_WhenNoUserHasOwnershipElsewhere()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var user1 = await userRepo.GetAsync(u => u.Id == userId1);
        var user2 = await userRepo.GetAsync(u => u.Id == userId2);

        // Create a group with both users
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "OwnerGroup",
            Users = [user1!, user2!],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        // Create owner role assigned only to this group
        var ownerRole = new RoleEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        // Neither user has ownership outside the group
        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [userId1, userId2],
            accountId,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroupAsync_ReturnsFalse_WhenEmptyUserList()
    {
        var accountId = await CreateAccountAsync();

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [],
            accountId,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroupAsync_ReturnsTrue_WhenLastUserInListHasOwnership()
    {
        var accountId = await CreateAccountAsync();
        var userId1 = await CreateUserInAccountAsync(accountId);
        var userId2 = await CreateUserInAccountAsync(accountId);
        var userId3 = await CreateUserInAccountAsync(accountId);

        // Create owner role with direct assignment for userId3 only (last in list)
        await CreateOwnerRoleAsync(accountId, directUserIds: [userId3], groupUserIds: []);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = _fixture.Create<int>(),
            AccountId = accountId,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [userId1, userId2, userId3],
            accountId,
            createdGroup.Id
        );

        Assert.True(result);
    }
}

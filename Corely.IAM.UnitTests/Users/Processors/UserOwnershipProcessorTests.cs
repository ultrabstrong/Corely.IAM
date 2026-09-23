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

    private async Task CreateOwnerRoleAsync(
        Guid accountId,
        Guid[] directUserIds,
        Guid[] groupUserIds
    )
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var directUsers = new List<UserEntity>();
        foreach (var userId in directUserIds)
        {
            var user = await userRepo.GetAsync(u => u.Id == userId);
            if (user != null)
            {
                directUsers.Add(user);
            }
        }

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
                Id = Guid.CreateVersion7(),
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
            Id = Guid.CreateVersion7(),
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
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenUserDoesNotHaveOwnerRole()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenUserHasOwnerRoleButNotInAccount()
    {
        var account = await CreateAccountAsync();
        var otherAccount = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(otherAccount.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [], groupUserIds: []);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsTrue_WhenUserIsSoleOwnerDirectly()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user.Id], groupUserIds: []);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user.Id, account.Id);

        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsTrue_WhenUserIsSoleOwnerViaGroup()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [], groupUserIds: [user.Id]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user.Id, account.Id);

        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenOtherOwnerExistsDirectly()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(
            account.Id,
            directUserIds: [user1.Id, user2.Id],
            groupUserIds: []
        );

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user1.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenOtherOwnerExistsViaGroup()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user1.Id], groupUserIds: [user2.Id]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user1.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenUserHasOwnerViaGroupAndOtherOwnerExistsDirectly()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user2.Id], groupUserIds: [user1.Id]);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user1.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsTrue_WhenOtherOwnerExistsButNotInAccount()
    {
        var account = await CreateAccountAsync();
        var otherAccount = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(otherAccount.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [user1!, user2!],
            Groups = [],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user1.Id, account.Id);

        Assert.True(result.IsSoleOwner);
        Assert.True(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task IsSoleOwnerOfAccount_ReturnsFalse_WhenNoOwnerRoleExistsForAccount()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var regularRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "RegularRole",
            IsSystemDefined = false,
            Users = [user!],
            Groups = [],
            Permissions = [],
        };
        await roleRepo.CreateAsync(regularRole);

        var result = await _userOwnershipProcessor.IsSoleOwnerOfAccountAsync(user.Id, account.Id);

        Assert.False(result.IsSoleOwner);
        Assert.False(result.UserHasOwnerRole);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroup_ReturnsTrue_WhenUserHasDirectOwnership()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user.Id], groupUserIds: []);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            user.Id,
            account.Id,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroup_ReturnsTrue_WhenUserHasOwnershipViaOtherGroup()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group1 = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "Group1",
            Users = [user!],
            Roles = [],
        };
        var createdGroup1 = await groupRepo.CreateAsync(group1);

        var group2 = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "Group2",
            Users = [user!],
            Roles = [],
        };
        var createdGroup2 = await groupRepo.CreateAsync(group2);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [],
            Groups = [createdGroup2],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            user.Id,
            account.Id,
            createdGroup1.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroup_ReturnsFalse_WhenOwnershipOnlyViaExcludedGroup()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "OwnerGroup",
            Users = [user!],
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

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            user.Id,
            account.Id,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroup_ReturnsFalse_WhenUserHasNoOwnerRole()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            user.Id,
            account.Id,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task HasOwnershipOutsideGroup_ReturnsTrue_WhenUserHasBothDirectAndGroupOwnership()
    {
        var account = await CreateAccountAsync();
        var user = await CreateUserInAccountAsync(account.Id);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "OwnerGroup",
            Users = [user!],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var ownerRole = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = RoleConstants.OWNER_ROLE_NAME,
            IsSystemDefined = true,
            Users = [user!],
            Groups = [createdGroup],
            Permissions = [],
        };
        await roleRepo.CreateAsync(ownerRole);

        var result = await _userOwnershipProcessor.HasOwnershipOutsideGroupAsync(
            user.Id,
            account.Id,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroup_ReturnsTrue_WhenOneUserHasDirectOwnership()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user1.Id], groupUserIds: []);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [user1.Id, user2.Id],
            account.Id,
            createdGroup.Id
        );

        Assert.True(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroup_ReturnsFalse_WhenNoUserHasOwnershipElsewhere()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();

        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "OwnerGroup",
            Users = [user1!, user2!],
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

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [user1.Id, user2.Id],
            account.Id,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroup_ReturnsFalse_WhenEmptyUserList()
    {
        var account = await CreateAccountAsync();

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [],
            account.Id,
            createdGroup.Id
        );

        Assert.False(result);
    }

    [Fact]
    public async Task AnyUserHasOwnershipOutsideGroup_ReturnsTrue_WhenLastUserInListHasOwnership()
    {
        var account = await CreateAccountAsync();
        var user1 = await CreateUserInAccountAsync(account.Id);
        var user2 = await CreateUserInAccountAsync(account.Id);
        var user3 = await CreateUserInAccountAsync(account.Id);

        await CreateOwnerRoleAsync(account.Id, directUserIds: [user3.Id], groupUserIds: []);

        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var group = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Name = "TestGroup",
            Users = [],
            Roles = [],
        };
        var createdGroup = await groupRepo.CreateAsync(group);

        var result = await _userOwnershipProcessor.AnyUserHasOwnershipOutsideGroupAsync(
            [user1.Id, user2.Id, user3.Id],
            account.Id,
            createdGroup.Id
        );

        Assert.True(result);
    }
}

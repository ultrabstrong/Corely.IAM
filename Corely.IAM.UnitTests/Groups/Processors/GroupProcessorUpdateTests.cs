using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Groups.Processors;

public class GroupProcessorUpdateTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly GroupProcessor _groupProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public GroupProcessorUpdateTests()
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

        _groupProcessor = new GroupProcessor(
            _serviceFactory.GetRequiredService<IRepo<GroupEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IUserOwnershipProcessor>(),
            _serviceFactory.GetRequiredService<IUserContextProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<GroupProcessor>>()
        );
    }

    private async Task<GroupEntity> CreateGroupEntityAsync(
        string name,
        Guid? accountId = null,
        string? description = null
    )
    {
        var groupRepo = _serviceFactory.GetRequiredService<IRepo<GroupEntity>>();
        var entity = new GroupEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            AccountId = accountId ?? _accountId,
            Users = [],
            Roles = [],
        };
        return await groupRepo.CreateAsync(entity);
    }

    [Fact]
    public async Task UpdateGroupAsync_UpdatesGroupNameAndDescription()
    {
        var group = await CreateGroupEntityAsync("OriginalName", description: "OriginalDesc");

        var request = new UpdateGroupRequest(group.Id, "UpdatedName", "UpdatedDesc");
        var result = await _groupProcessor.UpdateGroupAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);

        var getResult = await _groupProcessor.GetGroupByIdAsync(group.Id, false);
        Assert.NotNull(getResult.Data);
        Assert.Equal("UpdatedName", getResult.Data.Name);
        Assert.Equal("UpdatedDesc", getResult.Data.Description);
    }

    [Fact]
    public async Task UpdateGroupAsync_ReturnsNotFound_WhenGroupDoesNotExist()
    {
        var request = new UpdateGroupRequest(Guid.CreateVersion7(), "SomeName", null);
        var result = await _groupProcessor.UpdateGroupAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateGroupAsync_ReturnsNotFound_WhenGroupInDifferentAccount()
    {
        var otherAccountId = Guid.CreateVersion7();
        var group = await CreateGroupEntityAsync("OtherGroup", otherAccountId);

        var request = new UpdateGroupRequest(group.Id, "NewName", null);
        var result = await _groupProcessor.UpdateGroupAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateGroupAsync_ThrowsValidation_WhenNameEmpty()
    {
        var group = await CreateGroupEntityAsync("TestGroup");

        var request = new UpdateGroupRequest(group.Id, "", null);
        await Assert.ThrowsAsync<ValidationException>(() =>
            _groupProcessor.UpdateGroupAsync(request)
        );
    }
}

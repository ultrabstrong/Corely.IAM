using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Roles.Processors;

public class RoleProcessorUpdateTests
{
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly ServiceFactory _serviceFactory = new();
    private readonly Mock<IUserContextProvider> _mockUserContextProvider = new();
    private readonly RoleProcessor _roleProcessor;

    public RoleProcessorUpdateTests()
    {
        var userContext = new UserContext(
            new User
            {
                Id = Guid.CreateVersion7(),
                Username = "testuser",
                Email = "test@test.com",
            },
            new Account { Id = _accountId, AccountName = "TestAccount" },
            "device1",
            []
        );
        _mockUserContextProvider.Setup(x => x.GetUserContext()).Returns(userContext);

        _roleProcessor = new RoleProcessor(
            _serviceFactory.GetRequiredService<IRepo<RoleEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<AccountEntity>>(),
            _serviceFactory.GetRequiredService<IReadonlyRepo<PermissionEntity>>(),
            _mockUserContextProvider.Object,
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<RoleProcessor>>()
        );
    }

    private async Task<RoleEntity> CreateRoleEntityAsync(
        string name = "TestRole",
        string? description = null,
        bool isSystemDefined = false,
        Guid? accountId = null
    )
    {
        var effectiveAccountId = accountId ?? _accountId;
        var roleRepo = _serviceFactory.GetRequiredService<IRepo<RoleEntity>>();
        var entity = new RoleEntity
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            IsSystemDefined = isSystemDefined,
            AccountId = effectiveAccountId,
            Account = new AccountEntity { Id = effectiveAccountId },
            Users = [],
            Groups = [],
            Permissions = [],
        };
        return await roleRepo.CreateAsync(entity);
    }

    [Fact]
    public async Task UpdateRole_UpdatesRoleNameAndDescription()
    {
        var created = await CreateRoleEntityAsync();

        var request = new UpdateRoleRequest(
            created.Id,
            _accountId,
            "UpdatedRole",
            "Updated description"
        );
        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);

        var getResult = await _roleProcessor.GetRoleAsync(created.Id);
        Assert.NotNull(getResult.Role);
        Assert.Equal("UpdatedRole", getResult.Role.Name);
        Assert.Equal("Updated description", getResult.Role.Description);
    }

    [Fact]
    public async Task UpdateRole_UsesRequestAccountId_WhenCurrentAccountMissing()
    {
        var created = await CreateRoleEntityAsync();
        _mockUserContextProvider
            .Setup(x => x.GetUserContext())
            .Returns(new UserContext(true, "device1"));

        var request = new UpdateRoleRequest(
            created.Id,
            _accountId,
            "UpdatedRole",
            "Updated description"
        );
        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);

        var getResult = await _roleProcessor.GetRoleAsync(created.Id);
        Assert.NotNull(getResult.Role);
        Assert.Equal("UpdatedRole", getResult.Role.Name);
        Assert.Equal("Updated description", getResult.Role.Description);
    }

    [Fact]
    public async Task UpdateRole_ReturnsNotFound_WhenRoleDoesNotExist()
    {
        var request = new UpdateRoleRequest(Guid.CreateVersion7(), _accountId, "NoRole", null);

        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateRole_ReturnsNotFound_WhenRoleInDifferentAccount()
    {
        var otherAccountId = Guid.CreateVersion7();
        var created = await CreateRoleEntityAsync(accountId: otherAccountId);

        var request = new UpdateRoleRequest(created.Id, _accountId, "UpdatedRole", null);
        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateRole_ReturnsSystemDefinedError_WhenRoleIsSystemDefined()
    {
        var created = await CreateRoleEntityAsync(name: "Owner", isSystemDefined: true);

        var request = new UpdateRoleRequest(created.Id, _accountId, "RenamedOwner", null);
        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.SystemDefinedError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateRole_ReturnsValidationError_WhenNameEmpty()
    {
        var created = await CreateRoleEntityAsync();
        var request = new UpdateRoleRequest(created.Id, _accountId, "", null);

        var result = await _roleProcessor.UpdateRoleAsync(request);

        Assert.Equal(ModifyResultCode.ValidationError, result.ResultCode);
    }
}

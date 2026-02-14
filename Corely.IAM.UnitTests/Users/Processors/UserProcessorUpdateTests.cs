using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Corely.IAM.Validators;
using Corely.Security.Encryption.Factories;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.Users.Processors;

public class UserProcessorUpdateTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserProcessor _userProcessor;
    private readonly Guid _accountId = Guid.CreateVersion7();

    public UserProcessorUpdateTests()
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

    private async Task<UserEntity> CreateUserEntityAsync(
        string username = "originaluser",
        string email = "original@test.com"
    )
    {
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var entity = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Email = email,
            Accounts = [],
            Groups = [],
            Roles = [],
        };
        return await userRepo.CreateAsync(entity);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUsernameAndEmail()
    {
        var created = await CreateUserEntityAsync();

        var request = new UpdateUserRequest(created.Id, "updateduser", "updated@test.com");
        var result = await _userProcessor.UpdateUserAsync(request);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);

        var getResult = await _userProcessor.GetUserAsync(created.Id);
        Assert.NotNull(getResult.User);
        Assert.Equal("updateduser", getResult.User.Username);
        Assert.Equal("updated@test.com", getResult.User.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var request = new UpdateUserRequest(Guid.CreateVersion7(), "nouser", "no@test.com");

        var result = await _userProcessor.UpdateUserAsync(request);

        Assert.Equal(ModifyResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsValidation_WhenUsernameEmpty()
    {
        var created = await CreateUserEntityAsync();
        var request = new UpdateUserRequest(created.Id, "", "valid@test.com");

        await Assert.ThrowsAsync<ValidationException>(() =>
            _userProcessor.UpdateUserAsync(request)
        );
    }

    [Fact]
    public async Task UpdateUserAsync_ThrowsValidation_WhenEmailInvalid()
    {
        var created = await CreateUserEntityAsync();
        var request = new UpdateUserRequest(created.Id, "validuser", "not-an-email");

        await Assert.ThrowsAsync<ValidationException>(() =>
            _userProcessor.UpdateUserAsync(request)
        );
    }
}

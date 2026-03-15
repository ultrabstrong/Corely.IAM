using Corely.DataAccess.Interfaces.Repos;
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

public class UserProcessorUpdateCollisionTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly UserProcessor _userProcessor;

    public UserProcessorUpdateCollisionTests()
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

    [Fact]
    public async Task UpdateUserAsync_ReturnsUsernameExistsError_WhenUsernameAlreadyTaken()
    {
        var user1Result = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("user1", "user1@test.com")
        );
        var user2Result = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("user2", "user2@test.com")
        );

        var updateResult = await _userProcessor.UpdateUserAsync(
            new UpdateUserRequest(user2Result.CreatedId, "user1", "user2@test.com")
        );

        Assert.Equal(ModifyResultCode.UsernameExistsError, updateResult.ResultCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsEmailExistsError_WhenEmailAlreadyTaken()
    {
        var user1Result = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("user1", "user1@test.com")
        );
        var user2Result = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("user2", "user2@test.com")
        );

        var updateResult = await _userProcessor.UpdateUserAsync(
            new UpdateUserRequest(user2Result.CreatedId, "user2", "user1@test.com")
        );

        Assert.Equal(ModifyResultCode.EmailExistsError, updateResult.ResultCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsSuccess_WhenUpdatingToOwnExistingValues()
    {
        var createResult = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("myuser", "myuser@test.com")
        );

        var updateResult = await _userProcessor.UpdateUserAsync(
            new UpdateUserRequest(createResult.CreatedId, "myuser", "myuser@test.com")
        );

        Assert.Equal(ModifyResultCode.Success, updateResult.ResultCode);
    }

    [Fact]
    public async Task UpdateUserAsync_ReturnsSuccess_WhenUsernameAndEmailAreUnique()
    {
        var createResult = await _userProcessor.CreateUserAsync(
            new CreateUserRequest("oldname", "old@test.com")
        );

        var updateResult = await _userProcessor.UpdateUserAsync(
            new UpdateUserRequest(createResult.CreatedId, "newname", "new@test.com")
        );

        Assert.Equal(ModifyResultCode.Success, updateResult.ResultCode);

        var getResult = await _userProcessor.GetUserAsync(createResult.CreatedId);
        Assert.Equal("newname", getResult.User!.Username);
        Assert.Equal("new@test.com", getResult.User.Email);
    }
}

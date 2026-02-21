using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.BasicAuths.Processors;

public class BasicAuthProcessorTests
{
    private const string VALID_PASSWORD = "Password1!";

    private readonly ServiceFactory _serviceFactory = new();
    private readonly BasicAuthProcessor _basicAuthProcessor;

    public BasicAuthProcessorTests()
    {
        _basicAuthProcessor = new BasicAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>(),
            _serviceFactory.GetRequiredService<IPasswordValidationProvider>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<ILogger<BasicAuthProcessor>>()
        );
    }

    [Fact]
    public async Task CreateBasicAuthAsync_ReturnsSuccess_WhenBasicAuthDoesNotExist()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        var result = await _basicAuthProcessor.CreateBasicAuthAsync(request);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.CreatedId);
        Assert.Equal(CreateBasicAuthResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task CreateBasicAuthAsync_ReturnsBasicAuthExistsError_WhenBasicAuthExists()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        await _basicAuthProcessor.CreateBasicAuthAsync(request);
        var result = await _basicAuthProcessor.CreateBasicAuthAsync(request);

        Assert.NotNull(result);
        Assert.Equal(CreateBasicAuthResultCode.BasicAuthExistsError, result.ResultCode);
    }

    [Fact]
    public async Task CreateBasicAuthAsync_ReturnsPasswordValidationError_WhenPasswordIsWeak()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), "password");

        var result = await _basicAuthProcessor.CreateBasicAuthAsync(request);

        Assert.Equal(CreateBasicAuthResultCode.PasswordValidationError, result.ResultCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Equal(Guid.Empty, result.CreatedId);
    }

    [Fact]
    public async Task CreateBasicAuthAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _basicAuthProcessor.CreateBasicAuthAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_ReturnsSuccess_WhenBasicAuthExists()
    {
        var createRequest = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        await _basicAuthProcessor.CreateBasicAuthAsync(createRequest);

        var updateRequest = new UpdateBasicAuthRequest(createRequest.UserId, "NewPassword1!");
        var result = await _basicAuthProcessor.UpdateBasicAuthAsync(updateRequest);

        Assert.NotNull(result);
        Assert.Equal(UpdateBasicAuthResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_ReturnsBasicAuthNotFoundError_WhenBasicAuthDoesNotExist()
    {
        var request = new UpdateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        var result = await _basicAuthProcessor.UpdateBasicAuthAsync(request);

        Assert.NotNull(result);
        Assert.Equal(UpdateBasicAuthResultCode.BasicAuthNotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_ReturnsPasswordValidationError_WhenPasswordIsWeak()
    {
        var createRequest = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        await _basicAuthProcessor.CreateBasicAuthAsync(createRequest);

        var updateRequest = new UpdateBasicAuthRequest(createRequest.UserId, "password");
        var result = await _basicAuthProcessor.UpdateBasicAuthAsync(updateRequest);

        Assert.Equal(UpdateBasicAuthResultCode.PasswordValidationError, result.ResultCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task UpdateBasicAuthAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _basicAuthProcessor.UpdateBasicAuthAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsValidTrue_WhenBasicAuthExists()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        await _basicAuthProcessor.CreateBasicAuthAsync(request);

        var verifyRequest = new VerifyBasicAuthRequest(request.UserId, VALID_PASSWORD);
        var result = await _basicAuthProcessor.VerifyBasicAuthAsync(verifyRequest);

        Assert.Equal(VerifyBasicAuthResultCode.Success, result.ResultCode);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsValidFalse_WhenPasswordIsIncorrect()
    {
        var request = new CreateBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);
        await _basicAuthProcessor.CreateBasicAuthAsync(request);

        var verifyRequest = new VerifyBasicAuthRequest(request.UserId, "password");
        var result = await _basicAuthProcessor.VerifyBasicAuthAsync(verifyRequest);

        Assert.Equal(VerifyBasicAuthResultCode.Success, result.ResultCode);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsUserNotFoundError_WhenBasicAuthDoesNotExist()
    {
        var request = new VerifyBasicAuthRequest(Guid.CreateVersion7(), VALID_PASSWORD);

        var result = await _basicAuthProcessor.VerifyBasicAuthAsync(request);

        Assert.Equal(VerifyBasicAuthResultCode.UserNotFoundError, result.ResultCode);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _basicAuthProcessor.VerifyBasicAuthAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }
}

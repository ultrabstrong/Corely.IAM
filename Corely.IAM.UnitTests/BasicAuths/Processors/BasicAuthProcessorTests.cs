using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Enums;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Password;
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
    public async Task UpsertBasicAuthAsync_ReturnsCreateResult_WhenBasicAuthDoesNotExist()
    {
        var request = new UpsertBasicAuthRequest(1, VALID_PASSWORD);
        var result = await _basicAuthProcessor.UpsertBasicAuthAsync(request);

        Assert.NotNull(result);
        Assert.Equal(UpsertBasicAuthResultCode.Success, result.ResultCode);
        Assert.Equal(UpsertType.Create, result.UpsertType);
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_ReturnsUpdateResult_WhenBasicAuthExists()
    {
        var request = new UpsertBasicAuthRequest(1, VALID_PASSWORD);
        await _basicAuthProcessor.UpsertBasicAuthAsync(request);
        var result = await _basicAuthProcessor.UpsertBasicAuthAsync(request);

        Assert.NotNull(result);
        Assert.Equal(UpsertBasicAuthResultCode.Success, result.ResultCode);
        Assert.Equal(UpsertType.Update, result.UpsertType);
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_Throws_WhenPasswordValidationFails()
    {
        var request = new UpsertBasicAuthRequest(1, "password");

        var ex = await Record.ExceptionAsync(() =>
            _basicAuthProcessor.UpsertBasicAuthAsync(request)
        );

        Assert.NotNull(ex);
        var pvex = Assert.IsType<PasswordValidationException>(ex);
        Assert.NotNull(pvex.PasswordValidationResult);
        Assert.False(pvex.PasswordValidationResult.IsSuccess);
        Assert.NotEmpty(pvex.PasswordValidationResult.ValidationFailures);
    }

    [Fact]
    public async Task UpsertBasicAuthAsync_Throws_WithNullRequest()
    {
        var ex = await Record.ExceptionAsync(() => _basicAuthProcessor.UpsertBasicAuthAsync(null!));

        Assert.NotNull(ex);
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsValidTrue_WhenBasicAuthExists()
    {
        var request = new UpsertBasicAuthRequest(1, VALID_PASSWORD);
        await _basicAuthProcessor.UpsertBasicAuthAsync(request);

        var verifyRequest = new VerifyBasicAuthRequest(1, VALID_PASSWORD);
        var result = await _basicAuthProcessor.VerifyBasicAuthAsync(verifyRequest);

        Assert.Equal(VerifyBasicAuthResultCode.Success, result.ResultCode);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsValidFalse_WhenPasswordIsIncorrect()
    {
        var request = new UpsertBasicAuthRequest(1, VALID_PASSWORD);
        await _basicAuthProcessor.UpsertBasicAuthAsync(request);

        var verifyRequest = new VerifyBasicAuthRequest(1, "password");
        var result = await _basicAuthProcessor.VerifyBasicAuthAsync(verifyRequest);

        Assert.Equal(VerifyBasicAuthResultCode.Success, result.ResultCode);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task VerifyBasicAuthAsync_ReturnsUserNotFoundError_WhenBasicAuthDoesNotExist()
    {
        var request = new VerifyBasicAuthRequest(1, VALID_PASSWORD);

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

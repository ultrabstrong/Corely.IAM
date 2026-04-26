using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.Models;
using Corely.IAM.PasswordRecoveries.Entities;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.PasswordRecoveries.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Validators;
using Corely.Security.Secrets;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.PasswordRecoveries.Processors;

public class PasswordRecoveryProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly PasswordRecoveryProcessor _processor;

    public PasswordRecoveryProcessorTests()
    {
        _processor = new PasswordRecoveryProcessor(
            _serviceFactory.GetRequiredService<IRepo<PasswordRecoveryEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<UserEntity>>(),
            _serviceFactory.GetRequiredService<BasicAuthProcessor>(),
            _serviceFactory.GetRequiredService<IAuthenticationProvider>(),
            _serviceFactory.GetRequiredService<Corely.Security.Hashing.Factories.IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<ISecretProvider>(),
            _serviceFactory.GetRequiredService<IValidationProvider>(),
            _serviceFactory.GetRequiredService<TimeProvider>(),
            _serviceFactory.GetRequiredService<ILogger<PasswordRecoveryProcessor>>()
        );
    }

    [Fact]
    public async Task RequestPasswordRecoveryAsync_ReturnsValidationError_ForInvalidEmail()
    {
        var result = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest("not-an-email")
        );

        Assert.Equal(RequestPasswordRecoveryResultCode.ValidationError, result.ResultCode);
        Assert.Null(result.RecoveryToken);
    }

    [Fact]
    public async Task RequestPasswordRecoveryAsync_ReturnsUserNotFoundError_ForUnknownEmail()
    {
        var result = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest("unknown@example.com")
        );

        Assert.Equal(RequestPasswordRecoveryResultCode.UserNotFoundError, result.ResultCode);
        Assert.Equal("User not found for email unknown@example.com", result.Message);
        Assert.Null(result.RecoveryToken);
    }

    [Fact]
    public async Task RequestPasswordRecoveryAsync_InvalidatesOlderPendingRecoveries()
    {
        var user = await RegisterUserAsync();

        var first = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(user.Email)
        );
        var second = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(user.Email)
        );

        Assert.NotNull(first.RecoveryToken);
        Assert.NotNull(second.RecoveryToken);

        var firstValidation = await _processor.ValidatePasswordRecoveryTokenAsync(
            new ValidatePasswordRecoveryTokenRequest(first.RecoveryToken!)
        );
        var secondValidation = await _processor.ValidatePasswordRecoveryTokenAsync(
            new ValidatePasswordRecoveryTokenRequest(second.RecoveryToken!)
        );

        Assert.Equal(
            ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryInvalidatedError,
            firstValidation.ResultCode
        );
        Assert.Equal(ValidatePasswordRecoveryTokenResultCode.Success, secondValidation.ResultCode);
    }

    [Fact]
    public async Task ResetPasswordWithRecoveryAsync_ResetsExistingPassword_AndClearsLockout()
    {
        var user = await RegisterUserAsync();
        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var authProcessor = _serviceFactory.GetRequiredService<IBasicAuthProcessor>();

        user.LockedUtc = DateTime.UtcNow;
        user.FailedLoginsSinceLastSuccess = 4;
        await userRepo.UpdateAsync(user);

        var requestResult = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(user.Email)
        );

        var resetResult = await _processor.ResetPasswordWithRecoveryAsync(
            new ResetPasswordWithRecoveryRequest(requestResult.RecoveryToken!, "N3wP@ssword!")
        );

        Assert.Equal(ResetPasswordWithRecoveryResultCode.Success, resetResult.ResultCode);

        var verifyResult = await authProcessor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(user.Id, "N3wP@ssword!")
        );
        Assert.Equal(VerifyBasicAuthResultCode.Success, verifyResult.ResultCode);
        Assert.True(verifyResult.IsValid);

        var updatedUser = await userRepo.GetAsync(u => u.Id == user.Id);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser!.LockedUtc);
        Assert.Equal(0, updatedUser.FailedLoginsSinceLastSuccess);
    }

    [Fact]
    public async Task ResetPasswordWithRecoveryAsync_CreatesPassword_ForGoogleOnlyUser()
    {
        var user = await RegisterGoogleOnlyUserAsync();
        var authProcessor = _serviceFactory.GetRequiredService<IBasicAuthProcessor>();

        var requestResult = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(user.Email)
        );

        var resetResult = await _processor.ResetPasswordWithRecoveryAsync(
            new ResetPasswordWithRecoveryRequest(requestResult.RecoveryToken!, "P@ssword123!")
        );

        Assert.Equal(ResetPasswordWithRecoveryResultCode.Success, resetResult.ResultCode);

        var verifyResult = await authProcessor.VerifyBasicAuthAsync(
            new VerifyBasicAuthRequest(user.Id, "P@ssword123!")
        );
        Assert.Equal(VerifyBasicAuthResultCode.Success, verifyResult.ResultCode);
        Assert.True(verifyResult.IsValid);
    }

    [Fact]
    public async Task ResetPasswordWithRecoveryAsync_RevokesExistingAuthTokens()
    {
        var user = await RegisterUserAsync();
        var authenticationProvider = _serviceFactory.GetRequiredService<IAuthenticationProvider>();

        var authTokenResult = await authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(user.Id, "recovery-test-device")
        );

        var requestResult = await _processor.RequestPasswordRecoveryAsync(
            new RequestPasswordRecoveryRequest(user.Email)
        );

        var resetResult = await _processor.ResetPasswordWithRecoveryAsync(
            new ResetPasswordWithRecoveryRequest(requestResult.RecoveryToken!, "R3setP@ss!")
        );

        Assert.Equal(ResetPasswordWithRecoveryResultCode.Success, resetResult.ResultCode);

        var validationResult = await authenticationProvider.ValidateUserAuthTokenAsync(
            authTokenResult.Token!
        );
        Assert.Equal(
            UserAuthTokenValidationResultCode.TokenValidationFailed,
            validationResult.ResultCode
        );
    }

    private async Task<UserEntity> RegisterUserAsync()
    {
        var registrationService = _serviceFactory.GetRequiredService<IRegistrationService>();
        var suffix = Guid.CreateVersion7().ToString("N")[..8];
        var username = $"user{suffix}";
        var email = $"{username}@example.com";

        var result = await registrationService.RegisterUserAsync(
            new RegisterUserRequest(username, email, "InitP@ssword1!")
        );

        Assert.Equal(RegisterUserResultCode.Success, result.ResultCode);

        var userRepo = _serviceFactory.GetRequiredService<IRepo<UserEntity>>();
        var user = await userRepo.GetAsync(u => u.Id == result.CreatedUserId);
        Assert.NotNull(user);
        return user!;
    }

    private async Task<UserEntity> RegisterGoogleOnlyUserAsync()
    {
        var user = await RegisterUserAsync();
        var googleAuthRepo = _serviceFactory.GetRequiredService<IRepo<GoogleAuthEntity>>();
        var basicAuthRepo = _serviceFactory.GetRequiredService<IRepo<BasicAuthEntity>>();

        await googleAuthRepo.CreateAsync(
            new GoogleAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                GoogleSubjectId = Guid.CreateVersion7().ToString("N"),
                Email = user.Email,
            }
        );

        var basicAuth = await basicAuthRepo.GetAsync(a => a.UserId == user.Id);
        Assert.NotNull(basicAuth);
        await basicAuthRepo.DeleteAsync(basicAuth!);

        return user;
    }
}

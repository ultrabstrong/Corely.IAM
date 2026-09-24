using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.PasswordRecoveries.Constants;
using Corely.IAM.PasswordRecoveries.Entities;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.PasswordRecoveries.Processors;

internal class PasswordRecoveryProcessor(
    IRepo<PasswordRecoveryEntity> passwordRecoveryRepo,
    IRepo<UserEntity> userRepo,
    BasicAuthProcessor basicAuthProcessor,
    IAuthenticationProvider authenticationProvider,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    IValidationProvider validationProvider,
    TimeProvider timeProvider,
    ILogger<PasswordRecoveryProcessor> logger
) : IPasswordRecoveryProcessor
{
    private readonly IRepo<PasswordRecoveryEntity> _passwordRecoveryRepo =
        passwordRecoveryRepo.ThrowIfNull(nameof(passwordRecoveryRepo));
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly BasicAuthProcessor _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(
        nameof(basicAuthProcessor)
    );
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<PasswordRecoveryProcessor> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new RequestPasswordRecoveryResult(
                RequestPasswordRecoveryResultCode.ValidationError,
                validation.Message,
                null
            );
        }

        var userEntity = await _userRepo.GetAsync(u => u.Email == request.Email);
        if (userEntity == null)
        {
            _logger.LogInformation(
                "Password recovery requested for unknown email {Email}",
                request.Email
            );
            return new RequestPasswordRecoveryResult(
                RequestPasswordRecoveryResultCode.UserNotFoundError,
                $"User not found for email {request.Email}",
                null
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        await InvalidatePendingRecoveriesAsync(userEntity.Id, utcNow);

        var secret = _secretProvider.CreateSecret();
        var recoveryEntity = new PasswordRecoveryEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userEntity.Id,
            SecretHash = secret
                .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
                .ToHashString()!,
            ExpiresUtc = utcNow.AddSeconds(PasswordRecoveryConstants.TOKEN_TTL_SECONDS),
        };

        await _passwordRecoveryRepo.CreateAsync(recoveryEntity);

        _logger.LogInformation(
            "Created password recovery {PasswordRecoveryId} for user {UserId}",
            recoveryEntity.Id,
            userEntity.Id
        );

        return new RequestPasswordRecoveryResult(
            RequestPasswordRecoveryResultCode.Success,
            string.Empty,
            new PasswordRecoveryToken(recoveryEntity.Id, secret).ToString()
        );
    }

    public async Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var (recoveryEntity, resultCode, message) = await ValidateRecoveryTokenAsync(request.Token);
        if (recoveryEntity == null)
        {
            return new ValidatePasswordRecoveryTokenResult(resultCode, message);
        }

        return new ValidatePasswordRecoveryTokenResult(
            ValidatePasswordRecoveryTokenResultCode.Success,
            string.Empty
        );
    }

    public async Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var (recoveryEntity, resultCode, message) = await ValidateRecoveryTokenAsync(request.Token);
        if (recoveryEntity == null)
        {
            return resultCode switch
            {
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryExpiredError =>
                    new ResetPasswordWithRecoveryResult(
                        ResetPasswordWithRecoveryResultCode.PasswordRecoveryExpiredError,
                        message
                    ),
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryAlreadyUsedError =>
                    new ResetPasswordWithRecoveryResult(
                        ResetPasswordWithRecoveryResultCode.PasswordRecoveryAlreadyUsedError,
                        message
                    ),
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryInvalidatedError =>
                    new ResetPasswordWithRecoveryResult(
                        ResetPasswordWithRecoveryResultCode.PasswordRecoveryInvalidatedError,
                        message
                    ),
                _ => new ResetPasswordWithRecoveryResult(
                    ResetPasswordWithRecoveryResultCode.PasswordRecoveryNotFoundError,
                    message
                ),
            };
        }

        var userEntity = await _userRepo.GetAsync(u => u.Id == recoveryEntity.UserId);
        if (userEntity == null)
        {
            return new ResetPasswordWithRecoveryResult(
                ResetPasswordWithRecoveryResultCode.PasswordRecoveryNotFoundError,
                "Password recovery not found"
            );
        }

        var setPasswordResult = await SetPasswordAsync(userEntity.Id, request.Password);
        if (
            setPasswordResult.ResultCode != ResetPasswordWithRecoveryResultCode.Success
            && setPasswordResult.ResultCode
                != ResetPasswordWithRecoveryResultCode.PasswordValidationError
            && setPasswordResult.ResultCode != ResetPasswordWithRecoveryResultCode.ValidationError
        )
        {
            return setPasswordResult;
        }

        if (setPasswordResult.ResultCode != ResetPasswordWithRecoveryResultCode.Success)
            return setPasswordResult;

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        recoveryEntity.CompletedUtc = utcNow;
        await _passwordRecoveryRepo.UpdateAsync(recoveryEntity);

        await InvalidatePendingRecoveriesAsync(userEntity.Id, utcNow, recoveryEntity.Id);
        await _authenticationProvider.RevokeAllUserAuthTokensAsync(userEntity.Id);

        userEntity.LockedUtc = null;
        userEntity.FailedLoginsSinceLastSuccess = 0;
        await _userRepo.UpdateAsync(userEntity);

        _logger.LogInformation(
            "Password recovery {PasswordRecoveryId} completed for user {UserId}",
            recoveryEntity.Id,
            userEntity.Id
        );

        return new ResetPasswordWithRecoveryResult(
            ResetPasswordWithRecoveryResultCode.Success,
            string.Empty
        );
    }

    private async Task InvalidatePendingRecoveriesAsync(
        Guid userId,
        DateTime utcNow,
        Guid? excludeRecoveryId = null
    )
    {
        await _passwordRecoveryRepo.ExecuteUpdateAsync(
            r =>
                r.UserId == userId
                && r.CompletedUtc == null
                && r.InvalidatedUtc == null
                && r.ExpiresUtc > utcNow
                && (!excludeRecoveryId.HasValue || r.Id != excludeRecoveryId.Value),
            s => s.SetProperty(r => r.InvalidatedUtc, utcNow)
        );
    }

    private async Task<(
        PasswordRecoveryEntity? RecoveryEntity,
        ValidatePasswordRecoveryTokenResultCode ResultCode,
        string Message
    )> ValidateRecoveryTokenAsync(string token)
    {
        if (!PasswordRecoveryToken.TryParse(token, out var recoveryToken))
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryNotFoundError,
                "Password recovery not found"
            );
        }

        var recoveryId = recoveryToken.RecoveryId;
        var recoveryEntity = await _passwordRecoveryRepo.GetAsync(r => r.Id == recoveryId);
        if (recoveryEntity == null)
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryNotFoundError,
                "Password recovery not found"
            );
        }

        var storedHash = recoveryEntity.SecretHash.ToHashedValue(_hashProviderFactory);
        if (!storedHash.Verify(recoveryToken.Secret))
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryNotFoundError,
                "Password recovery not found"
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (recoveryEntity.CompletedUtc != null)
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryAlreadyUsedError,
                "Password recovery has already been used"
            );
        }

        if (recoveryEntity.InvalidatedUtc != null)
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryInvalidatedError,
                "Password recovery has been invalidated"
            );
        }

        if (recoveryEntity.ExpiresUtc < utcNow)
        {
            return (
                null,
                ValidatePasswordRecoveryTokenResultCode.PasswordRecoveryExpiredError,
                "Password recovery has expired"
            );
        }

        return (recoveryEntity, ValidatePasswordRecoveryTokenResultCode.Success, string.Empty);
    }

    private async Task<ResetPasswordWithRecoveryResult> SetPasswordAsync(
        Guid userId,
        string password
    )
    {
        var createResult = await _basicAuthProcessor.CreateBasicAuthAsync(
            new CreateBasicAuthRequest(userId, password)
        );

        if (createResult.ResultCode != CreateBasicAuthResultCode.BasicAuthExistsError)
        {
            return createResult.ResultCode switch
            {
                CreateBasicAuthResultCode.Success => new ResetPasswordWithRecoveryResult(
                    ResetPasswordWithRecoveryResultCode.Success,
                    createResult.Message
                ),
                CreateBasicAuthResultCode.PasswordValidationError =>
                    new ResetPasswordWithRecoveryResult(
                        ResetPasswordWithRecoveryResultCode.PasswordValidationError,
                        createResult.Message
                    ),
                CreateBasicAuthResultCode.ValidationError => new ResetPasswordWithRecoveryResult(
                    ResetPasswordWithRecoveryResultCode.ValidationError,
                    createResult.Message
                ),
                _ => new ResetPasswordWithRecoveryResult(
                    ResetPasswordWithRecoveryResultCode.ValidationError,
                    createResult.Message
                ),
            };
        }

        var updateResult = await _basicAuthProcessor.UpdateBasicAuthAsync(
            new UpdateBasicAuthRequest(userId, password)
        );

        return updateResult.ResultCode switch
        {
            UpdateBasicAuthResultCode.Success => new ResetPasswordWithRecoveryResult(
                ResetPasswordWithRecoveryResultCode.Success,
                updateResult.Message
            ),
            UpdateBasicAuthResultCode.PasswordValidationError =>
                new ResetPasswordWithRecoveryResult(
                    ResetPasswordWithRecoveryResultCode.PasswordValidationError,
                    updateResult.Message
                ),
            UpdateBasicAuthResultCode.ValidationError => new ResetPasswordWithRecoveryResult(
                ResetPasswordWithRecoveryResultCode.ValidationError,
                updateResult.Message
            ),
            _ => new ResetPasswordWithRecoveryResult(
                ResetPasswordWithRecoveryResultCode.ValidationError,
                updateResult.Message
            ),
        };
    }
}

using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Mappers;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Enums;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Password;
using Corely.Security.PasswordValidation.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.BasicAuths.Processors;

internal class BasicAuthProcessor(
    IRepo<BasicAuthEntity> basicAuthRepo,
    IPasswordValidationProvider passwordValidationProvider,
    IHashProviderFactory hashProviderFactory,
    IValidationProvider validationProvider,
    ILogger<BasicAuthProcessor> logger
) : IBasicAuthProcessor
{
    private readonly IRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IPasswordValidationProvider _passwordValidationProvider =
        passwordValidationProvider.ThrowIfNull(nameof(passwordValidationProvider));
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly ILogger<BasicAuthProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<UpsertBasicAuthResult> UpsertBasicAuthAsync(UpsertBasicAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var basicAuth = request.ToBasicAuth(_hashProviderFactory);
        _validationProvider.ThrowIfInvalid(basicAuth);

        var passwordValidationResults = _passwordValidationProvider.ValidatePassword(
            request.Password
        );
        if (!passwordValidationResults.IsSuccess)
        {
            throw new PasswordValidationException(
                passwordValidationResults,
                "Password validation failed"
            );
        }

        var basicAuthEntity = basicAuth.ToEntity(_hashProviderFactory);

        var existingAuth = await _basicAuthRepo.GetAsync(e => e.UserId == basicAuthEntity.UserId);

        if (existingAuth?.Id == null)
        {
            _logger.LogDebug(
                "No existing basic auth for UserId {UserId}. Creating new",
                request.UserId
            );
            var created = await _basicAuthRepo.CreateAsync(basicAuthEntity);
            return new UpsertBasicAuthResult(
                UpsertBasicAuthResultCode.Success,
                string.Empty,
                created.Id,
                UpsertType.Create
            );
        }

        _logger.LogDebug("Found existing basic auth for UserId {UserId}. Updating", request.UserId);
        await _basicAuthRepo.UpdateAsync(basicAuthEntity);
        return new UpsertBasicAuthResult(
            UpsertBasicAuthResultCode.Success,
            string.Empty,
            existingAuth.Id,
            UpsertType.Update
        );
    }

    public async Task<VerifyBasicAuthResult> VerifyBasicAuthAsync(VerifyBasicAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var basicAuthEntity = await _basicAuthRepo.GetAsync(e => e.UserId == request.UserId);
        var basicAuth = basicAuthEntity?.ToModel(_hashProviderFactory);

        if (basicAuth == null)
        {
            _logger.LogInformation("No basic auth found for UserId {UserId}", request.UserId);
            return new VerifyBasicAuthResult(
                VerifyBasicAuthResultCode.UserNotFoundError,
                $"No basic auth found for UserId {request.UserId}",
                false
            );
        }

        var isValid = basicAuth.Password.Verify(request.Password);
        return new VerifyBasicAuthResult(VerifyBasicAuthResultCode.Success, string.Empty, isValid);
    }
}

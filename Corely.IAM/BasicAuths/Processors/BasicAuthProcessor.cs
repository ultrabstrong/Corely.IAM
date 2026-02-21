using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Mappers;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
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

    public async Task<CreateBasicAuthResult> CreateBasicAuthAsync(CreateBasicAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var existingAuth = await _basicAuthRepo.GetAsync(e => e.UserId == request.UserId);
        if (existingAuth != null)
        {
            _logger.LogInformation("Basic auth already exists for UserId {UserId}", request.UserId);
            return new CreateBasicAuthResult(
                CreateBasicAuthResultCode.BasicAuthExistsError,
                $"Basic auth already exists for UserId {request.UserId}",
                Guid.Empty
            );
        }

        var basicAuth = request.ToBasicAuth(_hashProviderFactory);
        _validationProvider.ThrowIfInvalid(basicAuth);

        var passwordValidationResults = _passwordValidationProvider.ValidatePassword(
            request.Password
        );
        if (!passwordValidationResults.IsSuccess)
        {
            return new CreateBasicAuthResult(
                CreateBasicAuthResultCode.PasswordValidationError,
                string.Join(" ", passwordValidationResults.ValidationFailures),
                Guid.Empty
            );
        }

        var basicAuthEntity = basicAuth.ToEntity();

        _logger.LogDebug("Creating basic auth for UserId {UserId}", request.UserId);
        basicAuthEntity.Id = Guid.CreateVersion7();
        var created = await _basicAuthRepo.CreateAsync(basicAuthEntity);
        return new CreateBasicAuthResult(
            CreateBasicAuthResultCode.Success,
            string.Empty,
            created.Id
        );
    }

    public async Task<UpdateBasicAuthResult> UpdateBasicAuthAsync(UpdateBasicAuthRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var existingAuth = await _basicAuthRepo.GetAsync(e => e.UserId == request.UserId);
        if (existingAuth == null)
        {
            _logger.LogInformation("No basic auth found for UserId {UserId}", request.UserId);
            return new UpdateBasicAuthResult(
                UpdateBasicAuthResultCode.BasicAuthNotFoundError,
                $"No basic auth found for UserId {request.UserId}"
            );
        }

        var basicAuth = request.ToBasicAuth(_hashProviderFactory);
        basicAuth.Id = existingAuth.Id;
        _validationProvider.ThrowIfInvalid(basicAuth);

        var passwordValidationResults = _passwordValidationProvider.ValidatePassword(
            request.Password
        );
        if (!passwordValidationResults.IsSuccess)
        {
            return new UpdateBasicAuthResult(
                UpdateBasicAuthResultCode.PasswordValidationError,
                string.Join(" ", passwordValidationResults.ValidationFailures)
            );
        }

        var basicAuthEntity = basicAuth.ToEntity();

        _logger.LogDebug("Updating basic auth for UserId {UserId}", request.UserId);
        await _basicAuthRepo.UpdateAsync(basicAuthEntity);
        return new UpdateBasicAuthResult(UpdateBasicAuthResultCode.Success, string.Empty);
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

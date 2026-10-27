using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Mappers;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.ApiKeys.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Validators;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.ApiKeys.Processors;

internal class ApiKeyProcessor(
    IRepo<ApiKeyEntity> apiKeyRepo,
    IRepo<ApiKeySecretEntity> secretRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<RoleEntity> roleRepo,
    IApiKeyAuthenticationProvider apiKeyAuthenticationProvider,
    ISecurityPolicyProvider securityPolicyProvider,
    IValidationProvider validationProvider,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    TimeProvider timeProvider,
    ILogger<ApiKeyProcessor> logger
) : IApiKeyProcessor
{
    private readonly IRepo<ApiKeyEntity> _apiKeyRepo = apiKeyRepo.ThrowIfNull(nameof(apiKeyRepo));
    private readonly IRepo<ApiKeySecretEntity> _secretRepo = secretRepo.ThrowIfNull(
        nameof(secretRepo)
    );
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<RoleEntity> _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
    private readonly IApiKeyAuthenticationProvider _apiKeyAuthenticationProvider =
        apiKeyAuthenticationProvider.ThrowIfNull(nameof(apiKeyAuthenticationProvider));
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
    );
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<ApiKeyProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<CreateApiKeyResult> CreateApiKeyAsync(CreateApiKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        if (!policy.ApiKeysEnabled)
        {
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.ApiKeysDisabledError,
                "API keys are disabled",
                Guid.Empty,
                null
            );
        }

        var apiKey = request.ToApiKey();
        var validation = _validationProvider.ValidateAndLog(apiKey);
        if (!validation.IsValid)
        {
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.ValidationError,
                validation.Message,
                Guid.Empty,
                null
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresUtc = policy.ApiKeySecretExpiresUtc(request.ExpiresUtc, utcNow);
        if (!policy.AllowsApiKeySecretExpiry(expiresUtc, utcNow))
        {
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.ValidationError,
                $"A secret must expire in the future and within {policy.ApiKeyMaxLifetimeDays} days",
                Guid.Empty,
                null
            );
        }

        var account = await _accountRepo.GetAsync(a => a.Id == apiKey.AccountId);
        if (account == null)
        {
            _logger.LogWarning("Account with Id {AccountId} not found", apiKey.AccountId);
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.AccountNotFoundError,
                $"Account with Id {apiKey.AccountId} not found",
                Guid.Empty,
                null
            );
        }

        if (!policy.AllowsApiKeysIn(account.IsPlatformAccount))
        {
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.ApiKeysDisabledError,
                "API keys are disabled for the platform account",
                Guid.Empty,
                null
            );
        }

        if (
            await _apiKeyRepo.AnyAsync(k =>
                k.AccountId == apiKey.AccountId && k.Name == apiKey.Name
            )
        )
        {
            _logger.LogWarning("API key with name {ApiKeyName} already exists", apiKey.Name);
            return new CreateApiKeyResult(
                CreateApiKeyResultCode.ApiKeyExistsError,
                $"API key with name {apiKey.Name} already exists",
                Guid.Empty,
                null
            );
        }

        apiKey.Id = Guid.CreateVersion7();
        await _apiKeyRepo.CreateAsync(apiKey.ToEntity());
        var key = await IssueSecretAsync(apiKey.Id, ApiKeySlot.Primary, expiresUtc, null);

        _logger.LogInformation(
            "Created API key {ApiKeyId} in account {AccountId}",
            apiKey.Id,
            apiKey.AccountId
        );
        return new CreateApiKeyResult(CreateApiKeyResultCode.Success, string.Empty, apiKey.Id, key);
    }

    public async Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(
        RotateApiKeySecretRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        if (!policy.ApiKeysEnabled)
        {
            return new RotateApiKeySecretResult(
                RotateApiKeySecretResultCode.ApiKeysDisabledError,
                "API keys are disabled",
                null
            );
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresUtc = policy.ApiKeySecretExpiresUtc(request.ExpiresUtc, utcNow);
        if (!policy.AllowsApiKeySecretExpiry(expiresUtc, utcNow))
        {
            return new RotateApiKeySecretResult(
                RotateApiKeySecretResultCode.ValidationError,
                $"A secret must expire in the future and within {policy.ApiKeyMaxLifetimeDays} days",
                null
            );
        }

        var entity = await _apiKeyRepo.GetAsync(
            AccountScope.Of(request.AccountId).ByIdPredicate<ApiKeyEntity>(request.ApiKeyId),
            include: q => q.Include(k => k.Secrets)
        );
        if (entity == null)
        {
            _logger.LogWarning("API key with Id {ApiKeyId} not found", request.ApiKeyId);
            return new RotateApiKeySecretResult(
                RotateApiKeySecretResultCode.ApiKeyNotFoundError,
                $"API key with Id {request.ApiKeyId} not found",
                null
            );
        }

        var key = await IssueSecretAsync(
            entity.Id,
            request.Slot,
            expiresUtc,
            entity.Secrets?.FirstOrDefault(s => s.Slot == request.Slot)
        );
        await _apiKeyAuthenticationProvider.RevokeApiKeySlotAuthTokensAsync(
            entity.Id,
            request.Slot
        );

        _logger.LogInformation(
            "Rotated the {Slot} secret of API key {ApiKeyId}",
            request.Slot,
            entity.Id
        );
        return new RotateApiKeySecretResult(
            RotateApiKeySecretResultCode.Success,
            string.Empty,
            key
        );
    }

    public async Task<ModifyResult> UpdateApiKeyAsync(UpdateApiKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var validation = _validationProvider.ValidateAndLog(request);
        if (!validation.IsValid)
        {
            return new ModifyResult(ModifyResultCode.ValidationError, validation.Message);
        }

        var entity = await _apiKeyRepo.GetAsync(
            AccountScope.Of(request.AccountId).ByIdPredicate<ApiKeyEntity>(request.ApiKeyId)
        );
        if (entity == null)
        {
            _logger.LogWarning("API key with Id {ApiKeyId} not found", request.ApiKeyId);
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                $"API key with Id {request.ApiKeyId} not found"
            );
        }

        var name = request.Name.Trim();
        if (
            await _apiKeyRepo.AnyAsync(k =>
                k.AccountId == request.AccountId && k.Name == name && k.Id != request.ApiKeyId
            )
        )
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"API key with name {name} already exists"
            );
        }

        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        await _apiKeyRepo.UpdateAsync(entity);
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<DeleteApiKeyResult> DeleteApiKeyAsync(Guid apiKeyId, AccountScope accountId)
    {
        var entity = await _apiKeyRepo.GetAsync(
            accountId.ByIdPredicate<ApiKeyEntity>(apiKeyId),
            include: q => q.Include(k => k.Roles).Include(k => k.Groups)
        );
        if (entity == null)
        {
            _logger.LogWarning("API key with Id {ApiKeyId} not found", apiKeyId);
            return new DeleteApiKeyResult(
                DeleteApiKeyResultCode.ApiKeyNotFoundError,
                $"API key with Id {apiKeyId} not found"
            );
        }

        entity.Roles?.Clear();
        entity.Groups?.Clear();
        await _apiKeyRepo.UpdateAsync(entity);
        await _apiKeyRepo.DeleteAsync(entity);

        _logger.LogInformation("Deleted API key {ApiKeyId}", apiKeyId);
        return new DeleteApiKeyResult(DeleteApiKeyResultCode.Success, string.Empty);
    }

    public async Task<AssignRolesToApiKeyResult> AssignRolesToApiKeyAsync(
        AssignRolesToApiKeyRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var entity = await _apiKeyRepo.GetAsync(
            request.AccountId.ByIdPredicate<ApiKeyEntity>(request.ApiKeyId),
            include: q => q.Include(k => k.Roles)
        );
        if (entity == null)
        {
            _logger.LogWarning("API key with Id {ApiKeyId} not found", request.ApiKeyId);
            return new AssignRolesToApiKeyResult(
                AssignRolesToApiKeyResultCode.ApiKeyNotFoundError,
                $"API key with Id {request.ApiKeyId} not found",
                0,
                request.RoleIds
            );
        }

        var heldRoleIds = entity.Roles?.Select(r => r.Id).ToList() ?? [];
        var roleEntities = await _roleRepo.ListAsync(r =>
            request.RoleIds.Contains(r.Id)
            && r.AccountId == entity.AccountId
            && !heldRoleIds.Contains(r.Id)
        );
        if (roleEntities.Count == 0)
        {
            _logger.LogWarning(
                "All role ids are invalid (not found, already assigned to API key, or from different account) : {@InvalidRoleIds}",
                request.RoleIds
            );
            return new AssignRolesToApiKeyResult(
                AssignRolesToApiKeyResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found, already assigned to API key, or from different account)",
                0,
                request.RoleIds
            );
        }

        entity.Roles ??= [];
        foreach (var role in roleEntities)
        {
            entity.Roles.Add(role);
        }
        await _apiKeyRepo.UpdateAsync(entity);

        var invalidRoleIds = request.RoleIds.Except(roleEntities.Select(r => r.Id)).ToList();
        return new AssignRolesToApiKeyResult(
            invalidRoleIds.Count > 0
                ? AssignRolesToApiKeyResultCode.PartialSuccess
                : AssignRolesToApiKeyResultCode.Success,
            invalidRoleIds.Count > 0
                ? "Some role ids are invalid (not found, already assigned to API key, or from different account)"
                : string.Empty,
            roleEntities.Count,
            invalidRoleIds
        );
    }

    public async Task<RemoveRolesFromApiKeyResult> RemoveRolesFromApiKeyAsync(
        RemoveRolesFromApiKeyRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var entity = await _apiKeyRepo.GetAsync(
            request.AccountId.ByIdPredicate<ApiKeyEntity>(request.ApiKeyId),
            include: q => q.Include(k => k.Roles)
        );
        if (entity == null)
        {
            _logger.LogWarning("API key with Id {ApiKeyId} not found", request.ApiKeyId);
            return new RemoveRolesFromApiKeyResult(
                RemoveRolesFromApiKeyResultCode.ApiKeyNotFoundError,
                $"API key with Id {request.ApiKeyId} not found",
                0,
                request.RoleIds
            );
        }

        var rolesToRemove = entity.Roles?.Where(r => request.RoleIds.Contains(r.Id)).ToList() ?? [];
        if (rolesToRemove.Count == 0)
        {
            return new RemoveRolesFromApiKeyResult(
                RemoveRolesFromApiKeyResultCode.InvalidRoleIdsError,
                "All role ids are invalid (not found or not assigned to API key)",
                0,
                request.RoleIds
            );
        }

        foreach (var role in rolesToRemove)
        {
            entity.Roles!.Remove(role);
        }
        await _apiKeyRepo.UpdateAsync(entity);

        var invalidRoleIds = request.RoleIds.Except(rolesToRemove.Select(r => r.Id)).ToList();
        return new RemoveRolesFromApiKeyResult(
            invalidRoleIds.Count > 0
                ? RemoveRolesFromApiKeyResultCode.PartialSuccess
                : RemoveRolesFromApiKeyResultCode.Success,
            invalidRoleIds.Count > 0
                ? "Some role ids are invalid (not found or not assigned to API key)"
                : string.Empty,
            rolesToRemove.Count,
            invalidRoleIds
        );
    }

    public async Task<ListResult<ApiKey>> ListApiKeysAsync(
        ListApiKeysRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    ) =>
        await ListQueryHelper.ExecuteListAsync(
            _apiKeyRepo,
            k => k.AccountId == request.AccountId,
            request.Filter,
            request.Order,
            request.Skip,
            request.Take,
            e => e.ToModel(),
            authorizedResourceIds
        );

    public async Task<GetResult<ApiKey>> GetApiKeyByIdAsync(
        Guid apiKeyId,
        bool hydrate,
        AccountScope accountId
    )
    {
        var entity = hydrate
            ? await _apiKeyRepo.GetAsync(
                accountId.ByIdPredicate<ApiKeyEntity>(apiKeyId),
                include: q => q.Include(k => k.Secrets).Include(k => k.Roles).Include(k => k.Groups)
            )
            : await _apiKeyRepo.GetAsync(
                accountId.ByIdPredicate<ApiKeyEntity>(apiKeyId),
                include: q => q.Include(k => k.Secrets)
            );

        if (entity == null)
        {
            _logger.LogInformation(
                "API key with Id {ApiKeyId} not found for account {AccountId}",
                apiKeyId,
                accountId
            );
            return new GetResult<ApiKey>(
                RetrieveResultCode.NotFoundError,
                $"API key with Id {apiKeyId} not found",
                null
            );
        }

        return new GetResult<ApiKey>(RetrieveResultCode.Success, string.Empty, entity.ToModel());
    }

    private async Task<string> IssueSecretAsync(
        Guid apiKeyId,
        ApiKeySlot slot,
        DateTime expiresUtc,
        ApiKeySecretEntity? existing
    )
    {
        var prefix = ApiKeyCredential.NewPrefix();
        while (await _secretRepo.AnyAsync(s => s.Prefix == prefix))
            prefix = ApiKeyCredential.NewPrefix();

        var secret = _secretProvider.CreateSecret();
        var hash = secret
            .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
            .ToHashString()!;

        if (existing == null)
        {
            await _secretRepo.CreateAsync(
                new ApiKeySecretEntity
                {
                    Id = Guid.CreateVersion7(),
                    ApiKeyId = apiKeyId,
                    Slot = slot,
                    Prefix = prefix,
                    SecretHash = hash,
                    ExpiresUtc = expiresUtc,
                }
            );
        }
        else
        {
            existing.Prefix = prefix;
            existing.SecretHash = hash;
            existing.ExpiresUtc = expiresUtc;
            existing.LastUsedUtc = null;
            existing.CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _secretRepo.UpdateAsync(existing);
        }

        return new ApiKeyCredential(prefix, secret).ToString();
    }
}

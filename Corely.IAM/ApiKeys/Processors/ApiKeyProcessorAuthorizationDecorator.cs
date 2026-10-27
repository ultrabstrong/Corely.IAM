using Corely.Common.Extensions;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.ApiKeys.Processors;

internal class ApiKeyProcessorAuthorizationDecorator(
    IApiKeyProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IApiKeyProcessor
{
    private readonly IApiKeyProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CreateApiKeyResult> CreateApiKeyAsync(CreateApiKeyRequest request) =>
        _authorizationProvider.HasAccountContext(request.OwnerAccountId)
        && await _authorizationProvider.IsAuthorizedAsync(AuthAction.Create, ApiKey.RESOURCE_TYPE)
            ? await _inner.CreateApiKeyAsync(request)
            : new CreateApiKeyResult(
                CreateApiKeyResultCode.UnauthorizedError,
                $"Unauthorized to create API key {request.Name}",
                Guid.Empty,
                null
            );

    public async Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(
        RotateApiKeySecretRequest request
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                ApiKey.RESOURCE_TYPE,
                request.ApiKeyId
            )
        )
        {
            return new RotateApiKeySecretResult(
                RotateApiKeySecretResultCode.UnauthorizedError,
                $"Unauthorized to rotate API key {request.ApiKeyId}",
                null
            );
        }

        if (!await CanGrantEverythingTheKeyHoldsAsync(request.ApiKeyId, request.AccountId))
        {
            return new RotateApiKeySecretResult(
                RotateApiKeySecretResultCode.UnauthorizedError,
                "Cannot take a secret for a key that holds permissions you do not hold",
                null
            );
        }

        return await _inner.RotateApiKeySecretAsync(request);
    }

    private async Task<bool> CanGrantEverythingTheKeyHoldsAsync(
        Guid apiKeyId,
        AccountScope accountId
    )
    {
        var apiKey = (await _inner.GetApiKeyByIdAsync(apiKeyId, true, accountId)).Data;
        if (apiKey == null)
            return true;

        var roleIds = (apiKey.Roles ?? []).Select(r => r.Id).ToList();
        if (roleIds.Count > 0 && !await _authorizationProvider.CanGrantRolesAsync(roleIds))
            return false;

        foreach (var group in apiKey.Groups ?? [])
        {
            if (!await _authorizationProvider.CanGrantGroupAsync(group.Id))
                return false;
        }

        return true;
    }

    public async Task<ModifyResult> UpdateApiKeyAsync(UpdateApiKeyRequest request) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            ApiKey.RESOURCE_TYPE,
            request.ApiKeyId
        )
            ? await _inner.UpdateApiKeyAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                $"Unauthorized to update API key {request.ApiKeyId}"
            );

    public async Task<DeleteApiKeyResult> DeleteApiKeyAsync(
        Guid apiKeyId,
        AccountScope accountId
    ) =>
        _authorizationProvider.HasAccountContext(accountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            ApiKey.RESOURCE_TYPE,
            apiKeyId
        )
            ? await _inner.DeleteApiKeyAsync(apiKeyId, accountId)
            : new DeleteApiKeyResult(
                DeleteApiKeyResultCode.UnauthorizedError,
                $"Unauthorized to delete API key {apiKeyId}"
            );

    public async Task<AssignRolesToApiKeyResult> AssignRolesToApiKeyAsync(
        AssignRolesToApiKeyRequest request
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId.Id)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Update,
                ApiKey.RESOURCE_TYPE,
                request.ApiKeyId
            )
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                Role.RESOURCE_TYPE,
                [.. request.RoleIds]
            )
        )
        {
            return new AssignRolesToApiKeyResult(
                AssignRolesToApiKeyResultCode.UnauthorizedError,
                $"Unauthorized to update API key {request.ApiKeyId} or read roles",
                0,
                []
            );
        }

        if (!await _authorizationProvider.CanGrantRolesAsync(request.RoleIds))
        {
            return new AssignRolesToApiKeyResult(
                AssignRolesToApiKeyResultCode.UnauthorizedError,
                "Cannot grant permissions you do not hold",
                0,
                []
            );
        }

        return await _inner.AssignRolesToApiKeyAsync(request);
    }

    public async Task<RemoveRolesFromApiKeyResult> RemoveRolesFromApiKeyAsync(
        RemoveRolesFromApiKeyRequest request
    ) =>
        _authorizationProvider.HasAccountContext(request.AccountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            ApiKey.RESOURCE_TYPE,
            request.ApiKeyId
        )
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Role.RESOURCE_TYPE,
            [.. request.RoleIds]
        )
            ? await _inner.RemoveRolesFromApiKeyAsync(request)
            : new RemoveRolesFromApiKeyResult(
                RemoveRolesFromApiKeyResultCode.UnauthorizedError,
                $"Unauthorized to update API key {request.ApiKeyId} or read roles",
                0,
                []
            );

    public async Task<ListResult<ApiKey>> ListApiKeysAsync(
        ListApiKeysRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    )
    {
        if (
            !_authorizationProvider.HasAccountContext(request.AccountId)
            || !await _authorizationProvider.IsAuthorizedAsync(
                AuthAction.Read,
                ApiKey.RESOURCE_TYPE
            )
        )
        {
            return new ListResult<ApiKey>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list API keys",
                null
            );
        }

        return await _inner.ListApiKeysAsync(
            request,
            await _authorizationProvider.GetAuthorizedResourceIdsAsync(
                AuthAction.Read,
                ApiKey.RESOURCE_TYPE
            )
        );
    }

    public async Task<GetResult<ApiKey>> GetApiKeyByIdAsync(
        Guid apiKeyId,
        bool hydrate,
        AccountScope accountId
    ) =>
        _authorizationProvider.HasAccountContext(accountId.Id)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            ApiKey.RESOURCE_TYPE,
            apiKeyId
        )
            ? await _inner.GetApiKeyByIdAsync(apiKeyId, hydrate, accountId)
            : new GetResult<ApiKey>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read API key {apiKeyId}",
                null
            );
}

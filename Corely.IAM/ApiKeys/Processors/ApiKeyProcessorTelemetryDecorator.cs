using Corely.Common.Extensions;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.ApiKeys.Processors;

internal class ApiKeyProcessorTelemetryDecorator(
    IApiKeyProcessor inner,
    ILogger<ApiKeyProcessorTelemetryDecorator> logger
) : IApiKeyProcessor
{
    private readonly IApiKeyProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<ApiKeyProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateApiKeyResult> CreateApiKeyAsync(CreateApiKeyRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.CreateApiKeyAsync(request)
        );

    public async Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(
        RotateApiKeySecretRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.RotateApiKeySecretAsync(request)
        );

    public async Task<ModifyResult> UpdateApiKeyAsync(UpdateApiKeyRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.UpdateApiKeyAsync(request),
            logResult: true
        );

    public async Task<DeleteApiKeyResult> DeleteApiKeyAsync(
        Guid apiKeyId,
        AccountScope accountId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            new { apiKeyId, accountId },
            () => _inner.DeleteApiKeyAsync(apiKeyId, accountId),
            logResult: true
        );

    public async Task<AssignRolesToApiKeyResult> AssignRolesToApiKeyAsync(
        AssignRolesToApiKeyRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.AssignRolesToApiKeyAsync(request),
            logResult: true
        );

    public async Task<RemoveRolesFromApiKeyResult> RemoveRolesFromApiKeyAsync(
        RemoveRolesFromApiKeyRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.RemoveRolesFromApiKeyAsync(request),
            logResult: true
        );

    public async Task<ListResult<ApiKey>> ListApiKeysAsync(
        ListApiKeysRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            request,
            () => _inner.ListApiKeysAsync(request, authorizedResourceIds),
            logResult: true
        );

    public async Task<GetResult<ApiKey>> GetApiKeyByIdAsync(
        Guid apiKeyId,
        bool hydrate,
        AccountScope accountId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ApiKeyProcessor),
            new
            {
                apiKeyId,
                hydrate,
                accountId,
            },
            () => _inner.GetApiKeyByIdAsync(apiKeyId, hydrate, accountId),
            logResult: true
        );
}

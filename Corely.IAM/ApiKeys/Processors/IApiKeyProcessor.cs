using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.ApiKeys.Processors;

internal interface IApiKeyProcessor
{
    Task<CreateApiKeyResult> CreateApiKeyAsync(CreateApiKeyRequest request);
    Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(RotateApiKeySecretRequest request);
    Task<ModifyResult> UpdateApiKeyAsync(UpdateApiKeyRequest request);
    Task<DeleteApiKeyResult> DeleteApiKeyAsync(Guid apiKeyId, AccountScope accountId);
    Task<AssignRolesToApiKeyResult> AssignRolesToApiKeyAsync(AssignRolesToApiKeyRequest request);
    Task<RemoveRolesFromApiKeyResult> RemoveRolesFromApiKeyAsync(
        RemoveRolesFromApiKeyRequest request
    );
    Task<ListResult<ApiKey>> ListApiKeysAsync(
        ListApiKeysRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    );
    Task<GetResult<ApiKey>> GetApiKeyByIdAsync(Guid apiKeyId, bool hydrate, AccountScope accountId);
}

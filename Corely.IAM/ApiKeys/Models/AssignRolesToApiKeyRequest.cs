using Corely.IAM.Security.Models;

namespace Corely.IAM.ApiKeys.Models;

internal record AssignRolesToApiKeyRequest(
    List<Guid> RoleIds,
    Guid ApiKeyId,
    AccountScope AccountId
);

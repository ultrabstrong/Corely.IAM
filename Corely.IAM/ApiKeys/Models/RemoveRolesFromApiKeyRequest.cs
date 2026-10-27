using Corely.IAM.Security.Models;

namespace Corely.IAM.ApiKeys.Models;

internal record RemoveRolesFromApiKeyRequest(
    List<Guid> RoleIds,
    Guid ApiKeyId,
    AccountScope AccountId
);

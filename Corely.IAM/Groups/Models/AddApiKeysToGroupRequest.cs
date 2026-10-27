using Corely.IAM.Security.Models;

namespace Corely.IAM.Groups.Models;

internal record AddApiKeysToGroupRequest(
    List<Guid> ApiKeyIds,
    Guid GroupId,
    AccountScope AccountId
);

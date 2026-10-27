namespace Corely.IAM.ApiKeys.Models;

public enum AssignRolesToApiKeyResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    ApiKeyNotFoundError,
    UnauthorizedError,
}

internal record AssignRolesToApiKeyResult(
    AssignRolesToApiKeyResultCode ResultCode,
    string Message,
    int AddedRoleCount,
    List<Guid> InvalidRoleIds
);

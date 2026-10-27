namespace Corely.IAM.ApiKeys.Models;

internal record RemoveRolesFromApiKeyResult(
    RemoveRolesFromApiKeyResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds
);

internal enum RemoveRolesFromApiKeyResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    ApiKeyNotFoundError,
    UnauthorizedError,
}

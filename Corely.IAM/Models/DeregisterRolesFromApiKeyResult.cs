namespace Corely.IAM.Models;

public record DeregisterRolesFromApiKeyResult(
    DeregisterRolesFromApiKeyResultCode ResultCode,
    string Message,
    int RemovedRoleCount,
    List<Guid> InvalidRoleIds
);

public enum DeregisterRolesFromApiKeyResultCode
{
    Success,
    PartialSuccess,
    InvalidRoleIdsError,
    ApiKeyNotFoundError,
    UnauthorizedError,
}

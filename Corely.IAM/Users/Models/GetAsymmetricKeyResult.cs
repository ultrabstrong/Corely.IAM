namespace Corely.IAM.Users.Models;

internal record GetAsymmetricKeyResult(
    GetAsymmetricKeyResultCode ResultCode,
    string Message,
    string? PublicKey
);

internal enum GetAsymmetricKeyResultCode
{
    Success,
    UserNotFoundError,
    KeyNotFoundError,
    UnauthorizedError,
}

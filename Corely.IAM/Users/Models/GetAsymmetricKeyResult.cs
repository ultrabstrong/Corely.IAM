namespace Corely.IAM.Users.Models;

public record GetAsymmetricKeyResult(
    GetAsymmetricKeyResultCode ResultCode,
    string Message,
    string? PublicKey
);

public enum GetAsymmetricKeyResultCode
{
    Success,
    UserNotFoundError,
    KeyNotFoundError,
    UnauthorizedError,
}

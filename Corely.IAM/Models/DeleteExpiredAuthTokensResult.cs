namespace Corely.IAM.Models;

public record DeleteExpiredAuthTokensResult(
    DeleteExpiredAuthTokensResultCode ResultCode,
    string Message,
    int DeletedCount
);

public enum DeleteExpiredAuthTokensResultCode
{
    Success,
    UnauthorizedError,
}

using Corely.IAM.Accounts.Models;

namespace Corely.IAM.ApiKeys.Models;

internal enum ApiKeyAuthTokenResultCode
{
    Success,
    InvalidApiKeyError,
    TokenValidationFailed,
    SessionExpiredError,
    RefreshTokenReusedError,
    RefreshTokenAlreadyRotatedError,
}

internal record ApiKeyAuthTokenResult(
    ApiKeyAuthTokenResultCode ResultCode,
    string? Token,
    Guid? TokenId,
    ApiKey? ApiKey,
    Account? Account,
    string? DeviceId,
    string? RefreshToken,
    DateTime? RefreshExpiresUtc
)
{
    internal static ApiKeyAuthTokenResult Failed(ApiKeyAuthTokenResultCode resultCode) =>
        new(resultCode, null, null, null, null, null, null, null);
}

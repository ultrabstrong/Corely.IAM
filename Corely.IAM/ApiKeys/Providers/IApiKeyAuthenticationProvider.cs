using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Security.Models;

namespace Corely.IAM.ApiKeys.Providers;

internal interface IApiKeyAuthenticationProvider
{
    Task<ApiKeyAuthTokenResult> GetApiKeyAuthTokenAsync(GetApiKeyAuthTokenRequest request);
    Task<ApiKeyAuthTokenResult> ValidateApiKeyAuthTokenAsync(string authToken);
    Task<ApiKeyAuthTokenResult> RenewApiKeyAuthTokenAsync(SessionTokenRenewal renewal);
    Task<bool> RevokeApiKeyAuthTokenByIdAsync(Guid apiKeyId, Guid tokenId);
    Task RevokeAllApiKeyAuthTokensAsync(Guid apiKeyId);
    Task RevokeApiKeySlotAuthTokensAsync(Guid apiKeyId, ApiKeySlot slot);
}

using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

internal interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, int? resourceId = null);
    bool IsAuthorizedForOwnUser(int requestUserId);
}

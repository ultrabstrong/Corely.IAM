using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

internal interface IAuthorizationProvider
{
    Task AuthorizeAsync(string resourceType, AuthAction action, int? resourceId = null);
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, int? resourceId = null);
}

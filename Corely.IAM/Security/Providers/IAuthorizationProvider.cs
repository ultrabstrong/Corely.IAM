using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params int[] resourceIds);
    bool IsAuthorizedForOwnUser(int requestUserId);
    bool HasUserContext();
    Task<bool> HasAccountContextAsync();
}

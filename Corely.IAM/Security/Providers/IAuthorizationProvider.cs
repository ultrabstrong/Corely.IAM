using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext();
}

internal interface IAuthorizationCacheClearer
{
    void ClearCache();
}

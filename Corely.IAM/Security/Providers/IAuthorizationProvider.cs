using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);
    bool IsNonSystemUserContext();
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext(Guid accountId);
}

/// <summary>
/// Lets a host discard cached permissions immediately rather than waiting out
/// <see cref="Corely.IAM.Security.Models.SecurityOptions.PermissionCacheTtlSeconds"/>. Intended for
/// hosts with a long-lived scope that know permissions have just changed.
/// </summary>
public interface IAuthorizationCacheClearer
{
    void ClearCache();
}

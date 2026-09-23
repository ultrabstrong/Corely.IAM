using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);

    // null = wildcard (everything); empty = nothing.
    Task<IReadOnlySet<Guid>?> GetAuthorizedResourceIdsAsync(AuthAction action, string resourceType);
    bool IsNonSystemUserContext();
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext(Guid accountId);
}

public interface IAuthorizationCacheClearer
{
    void ClearCache();
}

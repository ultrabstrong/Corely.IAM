using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);

    /// <summary>
    /// The resource ids of <paramref name="resourceType"/> the caller may act on, or
    /// <see langword="null"/> when the caller holds a wildcard permission and every resource is
    /// permitted. An empty set means none.
    /// </summary>
    /// <remarks>
    /// For scoping a query rather than checking a resource in hand. A list cannot name its rows
    /// before fetching them, so filtering afterwards makes the page size and the total count wrong;
    /// pushing these ids into the query keeps both correct.
    /// </remarks>
    Task<IReadOnlySet<Guid>?> GetAuthorizedResourceIdsAsync(AuthAction action, string resourceType);
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

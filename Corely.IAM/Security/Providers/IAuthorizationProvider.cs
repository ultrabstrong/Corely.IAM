using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);

    Task<IReadOnlySet<Guid>?> GetAuthorizedResourceIdsAsync(AuthAction action, string resourceType);

    Task<bool> CanGrantAsync(string resourceType, Guid resourceId, params AuthAction[] actions);
    Task<bool> CanGrantPermissionsAsync(IEnumerable<Guid> permissionIds);
    Task<bool> CanGrantRolesAsync(IEnumerable<Guid> roleIds);
    Task<bool> CanGrantGroupAsync(Guid groupId);
    Task<IReadOnlySet<AuthAction>> GetGrantableActionsAsync(string resourceType, Guid resourceId);
    Task<IReadOnlySet<Guid>> GetGrantablePermissionIdsAsync(IEnumerable<Guid> permissionIds);
    Task<IReadOnlySet<Guid>> GetGrantableRoleIdsAsync(IEnumerable<Guid> roleIds);
    Task<IReadOnlySet<Guid>> GetGrantableGroupIdsAsync(IEnumerable<Guid> groupIds);
    bool IsNonSystemUserContext();
    bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true);
    bool HasUserContext();
    bool HasAccountContext(Guid accountId);
}

public interface IAuthorizationCacheClearer
{
    void ClearCache();
}

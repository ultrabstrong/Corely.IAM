using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Exceptions;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Auth.Providers;

internal class AuthorizationProvider(
    IUserContextProvider userContextProvider,
    IReadonlyRepo<PermissionEntity> permissionRepo
) : IAuthorizationProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo;
    private IReadOnlyList<PermissionEntity>? _cachedPermissions;

    public async Task AuthorizeAsync(string resourceType, AuthAction action, int? resourceId = null)
    {
        var userContext =
            _userContextProvider.GetUserContext()
            ?? throw new AuthorizationException(resourceType, action.ToString(), resourceId);

        var permissions = await GetPermissionsAsync();

        var hasPermission = permissions.Any(p =>
            (
                p.ResourceType == PermissionConstants.ALL_RESOURCE_TYPES
                || p.ResourceType == resourceType
            )
            && (p.ResourceId == 0 || p.ResourceId == resourceId)
            && HasAction(p, action)
        );

        if (!hasPermission)
            throw new AuthorizationException(resourceType, action.ToString(), resourceId);
    }

    private async Task<IReadOnlyList<PermissionEntity>> GetPermissionsAsync()
    {
        if (_cachedPermissions != null)
            return _cachedPermissions;

        var userContext = _userContextProvider.GetUserContext()!;

        _cachedPermissions = await _permissionRepo.ListAsync(p =>
            p.Roles!.Any(r =>
                r.Users!.Any(u => u.Id == userContext.UserId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userContext.UserId))
            )
            && p.AccountId == userContext.AccountId
        );

        return _cachedPermissions;
    }

    private static bool HasAction(PermissionEntity permission, AuthAction action) =>
        action switch
        {
            AuthAction.Create => permission.Create,
            AuthAction.Read => permission.Read,
            AuthAction.Update => permission.Update,
            AuthAction.Delete => permission.Delete,
            AuthAction.Execute => permission.Execute,
            _ => false,
        };
}

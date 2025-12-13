using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Providers;

internal class AuthorizationProvider(
    IIamUserContextProvider userContextProvider,
    IReadonlyRepo<PermissionEntity> permissionRepo,
    ILogger<AuthorizationProvider> logger
) : IAuthorizationProvider
{
    private readonly IIamUserContextProvider _userContextProvider = userContextProvider;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo;
    private readonly ILogger<AuthorizationProvider> _logger = logger;
    private IReadOnlyList<PermissionEntity>? _cachedPermissions;

    public async Task<bool> IsAuthorizedAsync(
        AuthAction action,
        string resourceType,
        int? resourceId = null
    )
    {
        var userContext = _userContextProvider.GetUserContext();
        if (userContext == null)
        {
            _logger.LogInformation(
                "Authorization denied: No user context set for {Action} on {ResourceType}{ResourceId}",
                action,
                resourceType,
                resourceId.HasValue ? $" {resourceId}" : string.Empty
            );
            return false;
        }

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
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} lacks {Action} permission for {ResourceType}{ResourceId}",
                userContext.UserId,
                action,
                resourceType,
                resourceId.HasValue ? $" {resourceId}" : string.Empty
            );
        }

        return hasPermission;
    }

    public bool IsAuthorizedForOwnUser(int requestUserId)
    {
        var userContext = _userContextProvider.GetUserContext();
        var isAuthorized = userContext?.UserId == requestUserId;

        if (!isAuthorized)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} is not authorized to act on user {RequestUserId}",
                userContext?.UserId.ToString() ?? "null",
                requestUserId
            );
        }

        return isAuthorized;
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

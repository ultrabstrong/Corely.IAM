using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Providers;

internal class AuthorizationProvider(
    IUserContextProvider userContextProvider,
    IReadonlyRepo<PermissionEntity> permissionRepo,
    ILogger<AuthorizationProvider> logger
) : IAuthorizationProvider, IAuthorizationCacheClearer
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo;
    private readonly ILogger<AuthorizationProvider> _logger = logger;
    private IReadOnlyList<PermissionEntity>? _cachedPermissions;

    public async Task<bool> IsAuthorizedAsync(
        AuthAction action,
        string resourceType,
        params int[] resourceIds
    )
    {
        var resourceIdDisplay =
            resourceIds.Length > 0 ? $" [{string.Join(", ", resourceIds)}]" : string.Empty;
        if (
            !TryGetUserContext(
                out var userContext,
                $"{action} on {resourceType}{resourceIdDisplay}"
            )
        )
            return false;

        var permissions = await GetPermissionsAsync();

        var hasPermission = permissions.Any(p =>
            (
                p.ResourceType == PermissionConstants.ALL_RESOURCE_TYPES
                || p.ResourceType == resourceType
            )
            && (p.ResourceId == 0 || resourceIds.Length == 0 || resourceIds.Contains(p.ResourceId))
            && HasAction(p, action)
        );

        if (!hasPermission)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} lacks {Action} permission for {ResourceType}{ResourceIds}",
                userContext.User,
                action,
                resourceType,
                resourceIdDisplay
            );
        }

        return hasPermission;
    }

    public bool IsAuthorizedForOwnUser(int requestUserId, bool suppressLog = true)
    {
        if (!TryGetUserContext(out var userContext, $"act on user {requestUserId}"))
            return false;

        var isAuthorized = userContext.User.Id == requestUserId;

        if (!isAuthorized && !suppressLog)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} is not authorized to act on user {RequestUserId}",
                userContext.User.Id,
                requestUserId
            );
        }

        return isAuthorized;
    }

    public bool HasUserContext() => _userContextProvider.GetUserContext() != null;

    public bool HasAccountContext()
    {
        if (!TryGetUserContext(out var userContext, "check account context"))
            return false;

        if (userContext.CurrentAccount == null)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} is not signed in to an account",
                userContext.User
            );
            return false;
        }

        var hasAccountContext = userContext.AvailableAccounts.Any(a =>
            a.Id == userContext.CurrentAccount.Id
        );

        if (!hasAccountContext)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} does not have access to account {AccountId}",
                userContext.User,
                userContext.CurrentAccount
            );
        }

        return hasAccountContext;
    }

    public void ClearCache()
    {
        _cachedPermissions = null;
    }

    private bool TryGetUserContext(out UserContext userContext, string operation)
    {
        var context = _userContextProvider.GetUserContext();
        if (context == null)
        {
            _logger.LogInformation(
                "Authorization denied: No user context set for {Operation}",
                operation
            );
            userContext = null!;
            return false;
        }

        userContext = context;
        return true;
    }

    private async Task<IReadOnlyList<PermissionEntity>> GetPermissionsAsync()
    {
        if (_cachedPermissions != null)
            return _cachedPermissions;

        var userContext = _userContextProvider.GetUserContext()!;
        var contextUserId = userContext.User.Id;
        var contextAccountId = userContext.CurrentAccount?.Id;

        _cachedPermissions = await _permissionRepo.ListAsync(p =>
            p.Roles!.Any(r =>
                r.Users!.Any(u => u.Id == contextUserId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == contextUserId))
            )
            && p.AccountId == contextAccountId
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

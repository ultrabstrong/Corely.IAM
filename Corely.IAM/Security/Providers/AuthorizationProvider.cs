using Corely.Common.Extensions;
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
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly ILogger<AuthorizationProvider> _logger = logger.ThrowIfNull(nameof(logger));
    private IReadOnlyList<PermissionEntity>? _cachedPermissions;
    private Guid? _cachedAccountId;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public async Task<bool> IsAuthorizedAsync(
        AuthAction action,
        string resourceType,
        params Guid[] resourceIds
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

        if (userContext.IsSystemContext)
            return true;

        if (
            !TryGetUserId(
                userContext,
                $"{action} on {resourceType}{resourceIdDisplay}",
                out var userId
            )
        )
            return false;

        var permissions = await GetPermissionsAsync();

        var relevantPermissions = permissions
            .Where(p =>
                (
                    p.ResourceType == PermissionConstants.ALL_RESOURCE_TYPES
                    || p.ResourceType == resourceType
                ) && HasAction(p, action)
            )
            .ToList();

        // Check if user has a wildcard permission (ResourceId == Guid.Empty)
        var hasWildcardPermission = relevantPermissions.Any(p => p.ResourceId == Guid.Empty);

        // If no specific resource IDs requested, just need any relevant permission
        // If user has wildcard permission, all resources are authorized
        // Otherwise, verify ALL requested resource IDs have a matching permission
        var hasPermission =
            (hasWildcardPermission || resourceIds.Length == 0)
                ? relevantPermissions.Count > 0
                : resourceIds.All(id => relevantPermissions.Any(p => p.ResourceId == id));

        if (!hasPermission)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} lacks {Action} permission for {ResourceType}{ResourceIds}",
                userId,
                action,
                resourceType,
                resourceIdDisplay
            );
        }

        return hasPermission;
    }

    public bool IsNonSystemUserContext()
    {
        if (!TryGetUserContext(out var userContext, "perform self-operation"))
            return false;

        return !userContext.IsSystemContext;
    }

    public bool IsAuthorizedForOwnUser(Guid requestUserId, bool suppressLog = true)
    {
        if (!TryGetUserContext(out var userContext, $"act on user {requestUserId}"))
            return false;

        // System context is NOT a user — cannot perform self-operations
        if (userContext.IsSystemContext)
            return false;

        if (!TryGetUserId(userContext, $"act on user {requestUserId}", out var userId))
            return false;

        var isAuthorized = userId == requestUserId;

        if (!isAuthorized && !suppressLog)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} is not authorized to act on user {RequestUserId}",
                userId,
                requestUserId
            );
        }

        return isAuthorized;
    }

    public bool HasUserContext() => _userContextProvider.GetUserContext() != null;

    public bool HasAccountContext(Guid accountId)
    {
        if (!TryGetUserContext(out var userContext, $"check account context for {accountId}"))
            return false;

        if (userContext.IsSystemContext)
            return true;

        if (userContext.CurrentAccount == null || userContext.CurrentAccount.Id != accountId)
        {
            if (
                !TryGetUserId(userContext, $"check account context for {accountId}", out var userId)
            )
                return false;

            _logger.LogInformation(
                "Authorization denied: User {UserId} is not signed in to account {AccountId}",
                userId,
                accountId
            );
            return false;
        }

        var hasAccountAccess = userContext.AvailableAccounts.Any(a => a.Id == accountId);

        if (!hasAccountAccess)
        {
            if (!TryGetUserId(userContext, $"check account access for {accountId}", out var userId))
                return false;

            _logger.LogInformation(
                "Authorization denied: User {UserId} does not have access to account {AccountId}",
                userId,
                accountId
            );
        }

        return hasAccountAccess;
    }

    public void ClearCache()
    {
        _cachedPermissions = null;
        _cachedAccountId = null;
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
        var userContext = _userContextProvider.GetUserContext();

        // System context bypasses at IsAuthorizedAsync level, so this shouldn't be called
        // but return empty as a safe fallback
        if (userContext?.IsSystemContext == true)
            return [];

        var currentAccountId = userContext?.CurrentAccount?.Id;

        // Fast path - cache already populated for the same account
        if (_cachedPermissions is not null && _cachedAccountId == currentAccountId)
            return _cachedPermissions;

        // Serialize access to prevent concurrent DbContext usage
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedPermissions is not null && _cachedAccountId == currentAccountId)
                return _cachedPermissions;

            if (userContext == null)
                return [];

            if (!TryGetUserId(userContext, "load permissions", out var contextUserId))
                return [];

            var contextAccountId = userContext.CurrentAccount?.Id;

            _cachedPermissions = await _permissionRepo.ListAsync(p =>
                p.Roles!.Any(r =>
                    r.Users!.Any(u => u.Id == contextUserId)
                    || r.Groups!.Any(g => g.Users!.Any(u => u.Id == contextUserId))
                )
                && p.AccountId == contextAccountId
            );

            _cachedAccountId = contextAccountId;

            return _cachedPermissions;
        }
        finally
        {
            _cacheLock.Release();
        }
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

    private bool TryGetUserId(UserContext userContext, string operation, out Guid userId)
    {
        if (userContext.User != null)
        {
            userId = userContext.User.Id;
            return true;
        }

        _logger.LogInformation(
            "Authorization denied: No user attached to context for {Operation}",
            operation
        );
        userId = Guid.Empty;
        return false;
    }
}

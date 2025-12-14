using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
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
    IReadonlyRepo<AccountEntity> accountRepo,
    ILogger<AuthorizationProvider> logger
) : IAuthorizationProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo;
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo;
    private readonly ILogger<AuthorizationProvider> _logger = logger;
    private IReadOnlyList<PermissionEntity>? _cachedPermissions;
    private IReadOnlyList<int>? _cachedAccountIds;

    public async Task<bool> IsAuthorizedAsync(
        AuthAction action,
        string resourceType,
        int? resourceId = null
    )
    {
        if (
            !TryGetUserContext(
                out var userContext,
                $"{action} on {resourceType}{(resourceId.HasValue ? $" {resourceId}" : string.Empty)}"
            )
        )
            return false;

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
        if (!TryGetUserContext(out var userContext, $"act on user {requestUserId}"))
            return false;

        var isAuthorized = userContext.UserId == requestUserId;

        if (!isAuthorized)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} is not authorized to act on user {RequestUserId}",
                userContext.UserId,
                requestUserId
            );
        }

        return isAuthorized;
    }

    public async Task<bool> HasAccountContextAsync()
    {
        if (!TryGetUserContext(out var userContext, "check account context"))
            return false;

        var accountIds = await GetAccountIdsAsync();
        var hasAccountContext = accountIds.Any(id => id == userContext.AccountId);

        if (!hasAccountContext)
        {
            _logger.LogInformation(
                "Authorization denied: User {UserId} does not have access to account {AccountId}",
                userContext.UserId,
                userContext.AccountId
            );
        }

        return hasAccountContext;
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

        _cachedPermissions = await _permissionRepo.ListAsync(p =>
            p.Roles!.Any(r =>
                r.Users!.Any(u => u.Id == userContext.UserId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userContext.UserId))
            )
            && p.AccountId == userContext.AccountId
        );

        return _cachedPermissions;
    }

    private async Task<IReadOnlyList<int>> GetAccountIdsAsync()
    {
        if (_cachedAccountIds != null)
            return _cachedAccountIds;

        var userContext = _userContextProvider.GetUserContext()!;

        var accounts = await _accountRepo.ListAsync(a =>
            a.Users!.Any(u => u.Id == userContext.UserId)
        );

        _cachedAccountIds = accounts.Select(a => a.Id).ToList();

        return _cachedAccountIds;
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

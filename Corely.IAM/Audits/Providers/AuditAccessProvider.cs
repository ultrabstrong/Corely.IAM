using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Audits.Models;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Audits.Providers;

internal class AuditAccessProvider(
    IUserContextProvider userContextProvider,
    IReadonlyRepo<PermissionEntity> permissionRepo,
    ISecurityPolicyProvider securityPolicyProvider
) : IAuditAccessProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));

    public async Task<AuditAccess> GetAccessAsync(AuthAction action)
    {
        var context = _userContextProvider.GetUserContext();
        if (context is null)
            return AuditAccess.None;
        if (context.IsSystemContext)
            return AuditAccess.All;

        var held = await HeldAsync(context, action, AuditEntry.RESOURCE_TYPE);
        if (held is null)
            return AuditAccess.None;

        var everything = held.Any(p =>
            p.Account?.IsPlatformAccount == true && p.ResourceId == Guid.Empty
        );
        var accountIds = held.Where(p => p.ResourceId == Guid.Empty || p.ResourceId == p.AccountId)
            .Select(p => p.AccountId)
            .ToHashSet();

        return new AuditAccess(everything, accountIds, context.User?.Id);
    }

    public async Task<bool> HoldsInPlatformAccountAsync(AuthAction action, string resourceType)
    {
        var context = _userContextProvider.GetUserContext();
        if (context is null)
            return false;
        if (context.IsSystemContext)
            return true;

        return (await HeldAsync(context, action, resourceType))?.Any(p =>
                p.Account?.IsPlatformAccount == true && p.ResourceId == Guid.Empty
            ) == true;
    }

    private async Task<List<PermissionEntity>?> HeldAsync(
        UserContext context,
        AuthAction action,
        string resourceType
    )
    {
        List<PermissionEntity> permissions;
        if (context.ApiKey is { } apiKey)
        {
            var allowedTypes = (await _securityPolicyProvider.GetPolicyAsync()).ApiKeyResourceTypes;
            if (!allowedTypes.Contains(resourceType))
                return [];

            var accountId = context.CurrentAccount?.Id;
            permissions = await _permissionRepo.ListAsync(
                p =>
                    p.ResourceType == resourceType
                    && p.AccountId == accountId
                    && p.Roles!.Any(r =>
                        r.ApiKeys!.Any(k => k.Id == apiKey.Id)
                        || r.Groups!.Any(g => g.ApiKeys!.Any(k => k.Id == apiKey.Id))
                    ),
                include: q => q.Include(p => p.Account)
            );
        }
        else if (context.User is { } user)
        {
            permissions = await _permissionRepo.ListAsync(
                p =>
                    p.ResourceType == resourceType
                    && p.Roles!.Any(r =>
                        r.Users!.Any(u => u.Id == user.Id)
                        || r.Groups!.Any(g => g.Users!.Any(u => u.Id == user.Id))
                    ),
                include: q => q.Include(p => p.Account)
            );
        }
        else
        {
            return null;
        }

        return [.. permissions.Where(p => p.Allows(action))];
    }
}

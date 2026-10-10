using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Mappers;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Providers;
using Microsoft.EntityFrameworkCore;

namespace Corely.IAM.Audits.Providers;

internal class AuditAccessProvider(
    IUserContextProvider userContextProvider,
    IReadonlyRepo<PermissionEntity> permissionRepo
) : IAuditAccessProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );

    public async Task<AuditAccess> GetAccessAsync(AuthAction action)
    {
        var context = _userContextProvider.GetUserContext();
        if (context is null)
            return AuditAccess.None;
        if (context.IsSystemContext)
            return AuditAccess.All;
        if (context.User is not { } user)
            return AuditAccess.None;

        var held = await HeldAsync(user.Id, action, AuditConstants.AUDIT_RESOURCE_TYPE);
        var everything = held.Any(p =>
            p.Account?.IsPlatformAccount == true && p.ResourceId == Guid.Empty
        );
        var accountIds = held.Where(p => p.ResourceId == Guid.Empty || p.ResourceId == p.AccountId)
            .Select(p => p.AccountId)
            .ToHashSet();

        return new AuditAccess(everything, accountIds, user.Id);
    }

    public async Task<bool> HoldsInPlatformAccountAsync(AuthAction action, string resourceType)
    {
        var context = _userContextProvider.GetUserContext();
        if (context is null)
            return false;
        if (context.IsSystemContext)
            return true;
        if (context.User is not { } user)
            return false;

        return (await HeldAsync(user.Id, action, resourceType)).Any(p =>
            p.Account?.IsPlatformAccount == true && p.ResourceId == Guid.Empty
        );
    }

    private async Task<List<PermissionEntity>> HeldAsync(
        Guid userId,
        AuthAction action,
        string resourceType
    ) =>
        (
            await _permissionRepo.ListAsync(
                p =>
                    p.ResourceType == resourceType
                    && p.Roles!.Any(r =>
                        r.Users!.Any(u => u.Id == userId)
                        || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
                    ),
                include: q => q.Include(p => p.Account)
            )
        )
            .Where(p => p.Allows(action))
            .ToList();
}

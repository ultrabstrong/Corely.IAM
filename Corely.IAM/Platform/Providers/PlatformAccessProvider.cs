using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Entities;

namespace Corely.IAM.Platform.Providers;

internal class PlatformAccessProvider(
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<PermissionEntity> permissionRepo
) : IPlatformAccessProvider
{
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );

    public async Task<Guid?> GetPlatformAccountIdAsync() =>
        (await _accountRepo.GetAsync(a => a.IsPlatformAccount))?.Id;

    public async Task<bool> CanEnterAnyAccountAsync(Guid userId) =>
        await _permissionRepo.GetAsync(p =>
            p.Account != null
            && p.Account.IsPlatformAccount
            && p.ResourceType == PermissionConstants.ACCOUNT_RESOURCE_TYPE
            && p.ResourceId == Guid.Empty
            && p.Read
            && p.Roles!.Any(r =>
                r.Users!.Any(u => u.Id == userId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))
            )
        )
            is not null;
}

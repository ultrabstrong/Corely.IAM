using System.Security.Cryptography;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Models;
using Corely.IAM.Permissions;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Roles.Constants;
using Corely.IAM.Roles.Entities;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class PlatformService(
    IRegistrationService registrationService,
    IAuthenticationService authenticationService,
    IMfaService mfaService,
    ITotpProvider totpProvider,
    IRepo<AccountEntity> accountRepo,
    IRepo<RoleEntity> roleRepo,
    IRepo<PermissionEntity> permissionRepo,
    IResourceTypeRegistry resourceTypeRegistry,
    ILogger<PlatformService> logger
) : IPlatformService
{
    private const string BOOTSTRAP_DEVICE_ID = "platform-bootstrap";

    private readonly IRegistrationService _registrationService = registrationService.ThrowIfNull(
        nameof(registrationService)
    );
    private readonly IAuthenticationService _authenticationService =
        authenticationService.ThrowIfNull(nameof(authenticationService));
    private readonly IMfaService _mfaService = mfaService.ThrowIfNull(nameof(mfaService));
    private readonly ITotpProvider _totpProvider = totpProvider.ThrowIfNull(nameof(totpProvider));
    private readonly IRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IRepo<RoleEntity> _roleRepo = roleRepo.ThrowIfNull(nameof(roleRepo));
    private readonly IRepo<PermissionEntity> _permissionRepo = permissionRepo.ThrowIfNull(
        nameof(permissionRepo)
    );
    private readonly IResourceTypeRegistry _resourceTypeRegistry = resourceTypeRegistry.ThrowIfNull(
        nameof(resourceTypeRegistry)
    );
    private readonly ILogger<PlatformService> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<BootstrapPlatformResult> BootstrapPlatformAsync(
        BootstrapPlatformRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (await _accountRepo.GetAsync(a => a.IsPlatformAccount) is { } existing)
        {
            return Failed(
                BootstrapPlatformResultCode.AlreadyBootstrappedError,
                $"The platform account already exists: {existing.AccountName} ({existing.Id})"
            );
        }

        var password = GeneratePassword();
        var user = await _registrationService.RegisterUserAsync(
            new RegisterUserRequest(request.OwnerUsername, request.OwnerEmail, password)
        );
        if (user.ResultCode != RegisterUserResultCode.Success)
        {
            return Failed(BootstrapPlatformResultCode.OwnerCreationError, user.Message);
        }

        var signIn = await _authenticationService.SignInAsync(
            new SignInRequest(request.OwnerUsername, password, BOOTSTRAP_DEVICE_ID)
        );
        if (signIn.ResultCode != SignInResultCode.Success)
        {
            return Failed(
                BootstrapPlatformResultCode.OwnerSignInError,
                signIn.Message ?? "Sign in failed"
            );
        }

        try
        {
            var account = await _registrationService.RegisterAccountAsync(
                new RegisterAccountRequest(request.AccountName, user.CreatedUserId)
            );
            if (account.ResultCode != RegisterAccountResultCode.Success)
            {
                return Failed(BootstrapPlatformResultCode.AccountCreationError, account.Message);
            }

            var totp = await _mfaService.EnableTotpAsync();
            if (totp.ResultCode != EnableTotpResultCode.Success)
            {
                return Failed(BootstrapPlatformResultCode.TwoFactorEnrollmentError, totp.Message);
            }
            var confirmed = await _mfaService.ConfirmTotpAsync(
                new ConfirmTotpRequest(_totpProvider.GenerateCode(totp.Secret!))
            );
            if (confirmed.ResultCode != ConfirmTotpResultCode.Success)
            {
                return Failed(
                    BootstrapPlatformResultCode.TwoFactorEnrollmentError,
                    confirmed.Message
                );
            }

            var accountEntity = (
                await _accountRepo.GetAsync(a => a.Id == account.CreatedAccountId)
            )!;
            accountEntity.IsPlatformAccount = true;
            await _accountRepo.UpdateAsync(accountEntity);
            await CompletePlatformOwnerPermissionsAsync();

            _logger.LogInformation(
                "Bootstrapped the platform account {AccountId} owned by {UserId}",
                accountEntity.Id,
                user.CreatedUserId
            );

            return new BootstrapPlatformResult(
                BootstrapPlatformResultCode.Success,
                string.Empty,
                new PlatformOwnerCredentials(
                    accountEntity.Id,
                    accountEntity.AccountName,
                    request.OwnerUsername,
                    request.OwnerEmail,
                    password,
                    totp.Secret!,
                    totp.SetupUri!,
                    totp.RecoveryCodes ?? []
                )
            );
        }
        finally
        {
            await _authenticationService.SignOutAsync(
                new SignOutRequest(signIn.AuthTokenId!.Value.ToString())
            );
        }
    }

    public async Task<int> CompletePlatformOwnerPermissionsAsync()
    {
        var platformAccount = await _accountRepo.GetAsync(a => a.IsPlatformAccount);
        if (platformAccount is null)
            return 0;

        var ownerRole = (
            await _roleRepo.GetAsync(
                r =>
                    r.AccountId == platformAccount.Id
                    && r.IsSystemDefined
                    && r.Name == RoleConstants.OWNER_ROLE_NAME,
                include: q => q.Include(r => r.Permissions)
            )
        )!;

        var added = 0;
        foreach (var type in _resourceTypeRegistry.GetAll())
        {
            if (ownerRole.Permissions!.Any(p => IsFullAccess(p, type.Name)))
                continue;

            var permission = await _permissionRepo.GetAsync(
                p =>
                    p.AccountId == platformAccount.Id
                    && p.ResourceType == type.Name
                    && p.ResourceId == Guid.Empty
                    && p.Create
                    && p.Read
                    && p.Update
                    && p.Delete
                    && p.Execute,
                include: q => q.Include(p => p.Roles)
            );
            if (permission is null)
            {
                await _permissionRepo.CreateAsync(
                    new PermissionEntity
                    {
                        Id = Guid.CreateVersion7(),
                        AccountId = platformAccount.Id,
                        ResourceType = type.Name,
                        ResourceId = Guid.Empty,
                        Create = true,
                        Read = true,
                        Update = true,
                        Delete = true,
                        Execute = true,
                        Description = PermissionLabelProvider.GetName(
                            type.Name,
                            Guid.Empty,
                            true,
                            true,
                            true,
                            true,
                            true
                        ),
                        IsSystemDefined = true,
                        Roles = [ownerRole],
                    }
                );
            }
            else
            {
                permission.IsSystemDefined = true;
                permission.Roles!.Add(ownerRole);
                await _permissionRepo.UpdateAsync(permission);
            }
            added++;
        }

        if (added > 0)
        {
            _logger.LogInformation(
                "Gave the platform Owner role full access on {Count} resource types",
                added
            );
        }
        return added;
    }

    private static bool IsFullAccess(PermissionEntity p, string resourceType) =>
        p.ResourceType == resourceType
        && p.ResourceId == Guid.Empty
        && p.Create
        && p.Read
        && p.Update
        && p.Delete
        && p.Execute;

    private static string GeneratePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*-_=+?";
        char[] chars =
        [
            .. RandomNumberGenerator.GetItems<char>(upper, 8),
            .. RandomNumberGenerator.GetItems<char>(lower, 8),
            .. RandomNumberGenerator.GetItems<char>(digits, 8),
            .. RandomNumberGenerator.GetItems<char>(symbols, 8),
        ];
        RandomNumberGenerator.Shuffle<char>(chars);
        return new string(chars);
    }

    private static BootstrapPlatformResult Failed(
        BootstrapPlatformResultCode resultCode,
        string message
    ) => new(resultCode, message, null);
}

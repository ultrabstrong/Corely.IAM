using System.Security.Cryptography;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.FederatedIdentities.Processors;
using Corely.IAM.IdentityProviders.Entities;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Mappers;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.IdentityProviders.Providers;
using Corely.IAM.IdentityProviders.Types;
using Corely.IAM.Models;
using Corely.IAM.Permissions;
using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Providers;
using Corely.IAM.Platform.Entities;
using Corely.IAM.Platform.Mappers;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Roles.Entities;
using Corely.IAM.Roles.Models;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Validators;
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
    IRepo<PlatformSecuritySettingsEntity> securitySettingsRepo,
    ISecurityPolicyProvider securityPolicyProvider,
    ConfiguredSecurityPolicy configuredSecurityPolicy,
    SecurityPolicyCache securityPolicyCache,
    IValidationProvider validationProvider,
    IRepo<IdentityProviderEntity> identityProviderRepo,
    IIdentityProviderSettingsProvider identityProviderSettingsProvider,
    ConfiguredIdentityProviders configuredIdentityProviders,
    IdentityProviderSettingsCache identityProviderSettingsCache,
    IdentityProviderCredentialCipher identityProviderCredentialCipher,
    IdentityProviderCatalog identityProviderCatalog,
    IFederatedIdentityProcessor federatedIdentityProcessor,
    ILogger<PlatformService> logger
) : IPlatformService
{
    private readonly IRepo<IdentityProviderEntity> _identityProviderRepo =
        identityProviderRepo.ThrowIfNull(nameof(identityProviderRepo));
    private readonly IIdentityProviderSettingsProvider _identityProviderSettingsProvider =
        identityProviderSettingsProvider.ThrowIfNull(nameof(identityProviderSettingsProvider));
    private readonly ConfiguredIdentityProviders _configuredIdentityProviders =
        configuredIdentityProviders.ThrowIfNull(nameof(configuredIdentityProviders));
    private readonly IdentityProviderSettingsCache _identityProviderSettingsCache =
        identityProviderSettingsCache.ThrowIfNull(nameof(identityProviderSettingsCache));
    private readonly IdentityProviderCredentialCipher _identityProviderCredentialCipher =
        identityProviderCredentialCipher.ThrowIfNull(nameof(identityProviderCredentialCipher));
    private readonly IdentityProviderCatalog _identityProviderCatalog =
        identityProviderCatalog.ThrowIfNull(nameof(identityProviderCatalog));
    private readonly IFederatedIdentityProcessor _federatedIdentityProcessor =
        federatedIdentityProcessor.ThrowIfNull(nameof(federatedIdentityProcessor));

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
    private readonly IRepo<PlatformSecuritySettingsEntity> _securitySettingsRepo =
        securitySettingsRepo.ThrowIfNull(nameof(securitySettingsRepo));
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly ConfiguredSecurityPolicy _configuredSecurityPolicy =
        configuredSecurityPolicy.ThrowIfNull(nameof(configuredSecurityPolicy));
    private readonly SecurityPolicyCache _securityPolicyCache = securityPolicyCache.ThrowIfNull(
        nameof(securityPolicyCache)
    );
    private readonly IValidationProvider _validationProvider = validationProvider.ThrowIfNull(
        nameof(validationProvider)
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
            await SaveSecuritySettingsAsync(_configuredSecurityPolicy.Policy);
            await SaveConfiguredIdentityProvidersAsync();
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
                    && r.Name == Role.OWNER_ROLE_NAME,
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

    public async Task<GetSecuritySettingsResult> GetSecuritySettingsAsync() =>
        new(
            RetrieveResultCode.Success,
            string.Empty,
            await _securityPolicyProvider.GetPolicyAsync()
        );

    public async Task<ModifyResult> UpdateSecuritySettingsAsync(SecurityPolicy settings)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        var validation = _validationProvider.Validate(settings);
        if (!validation.IsValid)
            return new ModifyResult(ModifyResultCode.ValidationError, validation.Message);

        var unknownTypes = settings
            .ApiKeyResourceTypes.Types.Where(t => !_resourceTypeRegistry.Exists(t))
            .ToList();
        if (unknownTypes.Count > 0)
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"Not registered resource types: {string.Join(", ", unknownTypes)}"
            );

        if (!await _accountRepo.AnyAsync(a => a.IsPlatformAccount))
        {
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                "Security settings are saved in the platform account, and there is none; set them in configuration instead"
            );
        }

        await SaveSecuritySettingsAsync(settings);
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<GetIdentityProvidersResult> GetIdentityProvidersAsync()
    {
        var configured = (await _identityProviderSettingsProvider.GetAllAsync()).ToDictionary(s =>
            s.Type
        );
        return new(
            RetrieveResultCode.Success,
            string.Empty,
            [
                .. _identityProviderCatalog
                    .All.OrderBy(d => d.Type)
                    .Select(d =>
                        configured.TryGetValue(d.Type, out var settings)
                            ? settings
                            : new IdentityProviderSettings(
                                d.Type,
                                false,
                                IdentityProviderSettings.ALLOW_SIGN_UP_DEFAULT,
                                string.Empty,
                                d.CredentialKinds[0],
                                false,
                                null,
                                null
                            )
                    ),
            ]
        );
    }

    public async Task<ModifyResult> UpdateIdentityProviderAsync(
        UpdateIdentityProviderRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var validation = _validationProvider.Validate(request);
        if (!validation.IsValid)
            return new ModifyResult(ModifyResultCode.ValidationError, validation.Message);

        var definition = _identityProviderCatalog.Get(request.Type);
        if (!definition.CredentialKinds.Contains(request.CredentialKind))
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"{definition.DisplayName} does not take a {request.CredentialKind}"
            );
        }
        if (
            !string.IsNullOrWhiteSpace(request.Tenant)
            && request.Type != IdentityProviderType.Microsoft
        )
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"{definition.DisplayName} has no tenant setting"
            );
        }

        if (!await _accountRepo.AnyAsync(a => a.IsPlatformAccount))
        {
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                "Identity providers are saved in the platform account, and there is none; set them in configuration instead"
            );
        }

        var entity = await _identityProviderRepo.GetAsync(e => e.Type == request.Type);
        var isNew = entity is null;
        entity ??= new IdentityProviderEntity { Type = request.Type };
        entity.Apply(request);

        if (request.ReplacesCredential)
        {
            var credential = ReadCredential(request);
            if (credential.Error is not null)
                return new ModifyResult(ModifyResultCode.ValidationError, credential.Error);
            entity.EncryptedCredential = _identityProviderCredentialCipher.Encrypt(
                credential.Value!
            );
            entity.CertificateThumbprint = credential.Thumbprint;
        }

        if (entity.Enabled && string.IsNullOrWhiteSpace(entity.EncryptedCredential))
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"{definition.DisplayName} needs its {request.CredentialKind} before it can be turned on"
            );
        }

        if (isNew)
            await _identityProviderRepo.CreateAsync(entity);
        else
            await _identityProviderRepo.UpdateAsync(entity);
        _identityProviderSettingsCache.Invalidate();
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<CountSoleSignInUsersResult> CountSoleSignInUsersAsync(
        IdentityProviderType type
    ) =>
        new(
            RetrieveResultCode.Success,
            string.Empty,
            await _federatedIdentityProcessor.CountSoleSignInUsersAsync(type)
        );

    private static (string? Value, string? Thumbprint, string? Error) ReadCredential(
        UpdateIdentityProviderRequest request
    )
    {
        if (request.CredentialKind == IdentityProviderCredentialKind.ClientSecret)
        {
            return string.IsNullOrWhiteSpace(request.ClientSecret)
                ? (null, null, "A client secret is required")
                : (request.ClientSecret.Trim(), null, null);
        }

        if (request.CertificatePfx is not { Length: > 0 } pfx)
            return (null, null, "A certificate is required");
        try
        {
            var certificate = IdentityProviderCertificate.FromPfx(pfx, request.CertificatePassword);
            return (certificate.Pem, certificate.Thumbprint, null);
        }
        catch (Exception ex) when (ex is CryptographicException or InvalidOperationException)
        {
            return (
                null,
                null,
                "The certificate could not be read. Upload a PFX that includes its private key, with its password."
            );
        }
    }

    private async Task SaveConfiguredIdentityProvidersAsync()
    {
        foreach (var provider in _configuredIdentityProviders.Providers)
        {
            if (await _identityProviderRepo.AnyAsync(e => e.Type == provider.Settings.Type))
                continue;
            await _identityProviderRepo.CreateAsync(
                provider.ToEntity(_identityProviderCredentialCipher.Encrypt)
            );
        }
        _identityProviderSettingsCache.Invalidate();
    }

    private async Task SaveSecuritySettingsAsync(SecurityPolicy settings)
    {
        var entity = await _securitySettingsRepo.GetAsync(s =>
            s.Id == PlatformSecuritySettingsEntity.SINGLE_ROW_ID
        );
        if (entity is null)
        {
            await _securitySettingsRepo.CreateAsync(settings.ToEntity());
        }
        else
        {
            entity.Apply(settings);
            await _securitySettingsRepo.UpdateAsync(entity);
        }
        _securityPolicyCache.Invalidate();
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

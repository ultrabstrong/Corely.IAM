using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Processors;

internal class KeyRotationProcessor(
    IReadonlyRepo<UserEntity> userRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IRepo<UserSymmetricKeyEntity> userSymmetricKeyRepo,
    IRepo<UserAsymmetricKeyEntity> userAsymmetricKeyRepo,
    IRepo<AccountSymmetricKeyEntity> accountSymmetricKeyRepo,
    IRepo<AccountAsymmetricKeyEntity> accountAsymmetricKeyRepo,
    ISecurityProvider securityProvider,
    IUserContextProvider userContextProvider,
    ILogger<KeyRotationProcessor> logger
) : IKeyRotationProcessor
{
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IRepo<UserSymmetricKeyEntity> _userSymmetricKeyRepo =
        userSymmetricKeyRepo.ThrowIfNull(nameof(userSymmetricKeyRepo));
    private readonly IRepo<UserAsymmetricKeyEntity> _userAsymmetricKeyRepo =
        userAsymmetricKeyRepo.ThrowIfNull(nameof(userAsymmetricKeyRepo));
    private readonly IRepo<AccountSymmetricKeyEntity> _accountSymmetricKeyRepo =
        accountSymmetricKeyRepo.ThrowIfNull(nameof(accountSymmetricKeyRepo));
    private readonly IRepo<AccountAsymmetricKeyEntity> _accountAsymmetricKeyRepo =
        accountAsymmetricKeyRepo.ThrowIfNull(nameof(accountAsymmetricKeyRepo));
    private readonly ISecurityProvider _securityProvider = securityProvider.ThrowIfNull(
        nameof(securityProvider)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly ILogger<KeyRotationProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (!Enum.IsDefined(request.KeyType))
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"Unknown key type {request.KeyType}"
            );
        }

        var account = await _accountRepo.GetAsync(
            a => a.Id == request.AccountId,
            include: q => q.Include(a => a.SymmetricKeys).Include(a => a.AsymmetricKeys)
        );
        if (account == null)
        {
            _logger.LogInformation("Account with Id {AccountId} not found", request.AccountId);
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                $"Account with Id {request.AccountId} not found"
            );
        }

        int version;
        if (request.KeyType == KeyType.SymmetricEncryption)
        {
            var key = _securityProvider.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
            key.Version = account.SymmetricKeys.NextVersion(key.KeyUsedFor);
            await _accountSymmetricKeyRepo.CreateAsync(key.ToAccountEntity(account.Id));
            version = key.Version;
        }
        else
        {
            var key = NewAsymmetricKey(request.KeyType);
            key.Version = account.AsymmetricKeys.NextVersion(key.KeyUsedFor);
            await _accountAsymmetricKeyRepo.CreateAsync(key.ToAccountEntity(account.Id));
            version = key.Version;
        }

        _logger.LogInformation(
            "Rotated {KeyType} key for account {AccountId} to version {Version}",
            request.KeyType,
            account.Id,
            version
        );
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    public async Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType)
    {
        if (!Enum.IsDefined(keyType))
        {
            return new ModifyResult(
                ModifyResultCode.ValidationError,
                $"Unknown key type {keyType}"
            );
        }

        var userId =
            _userContextProvider.GetUserContext()?.User?.Id
            ?? throw new InvalidOperationException(
                "A non-system user context is required to rotate current user keys."
            );
        var user = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.SymmetricKeys).Include(u => u.AsymmetricKeys)
        );
        if (user == null)
        {
            _logger.LogInformation("User with Id {UserId} not found", userId);
            return new ModifyResult(
                ModifyResultCode.NotFoundError,
                $"User with Id {userId} not found"
            );
        }

        int version;
        if (keyType == KeyType.SymmetricEncryption)
        {
            var key = _securityProvider.GetSymmetricEncryptionKeyEncryptedWithSystemKey();
            key.Version = user.SymmetricKeys.NextVersion(key.KeyUsedFor);
            await _userSymmetricKeyRepo.CreateAsync(key.ToUserEntity(user.Id));
            version = key.Version;
        }
        else
        {
            var key = NewAsymmetricKey(keyType);
            key.Version = user.AsymmetricKeys.NextVersion(key.KeyUsedFor);
            await _userAsymmetricKeyRepo.CreateAsync(key.ToUserEntity(user.Id));
            version = key.Version;
        }

        _logger.LogInformation(
            "Rotated {KeyType} key for user {UserId} to version {Version}",
            keyType,
            user.Id,
            version
        );
        return new ModifyResult(ModifyResultCode.Success, string.Empty);
    }

    private AsymmetricKey NewAsymmetricKey(KeyType keyType) =>
        keyType == KeyType.AsymmetricSignature
            ? _securityProvider.GetAsymmetricSignatureKeyEncryptedWithSystemKey()
            : _securityProvider.GetAsymmetricEncryptionKeyEncryptedWithSystemKey();
}

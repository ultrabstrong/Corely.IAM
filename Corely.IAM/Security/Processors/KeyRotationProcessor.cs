using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.DataAccess.Interfaces.UnitOfWork;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Models.Extensions;
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
    ILogger<KeyRotationProcessor> logger,
    TimeProvider timeProvider,
    IReauthenticationProvider reauthenticationProvider,
    IUnitOfWorkProvider uowProvider
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
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly IReauthenticationProvider _reauthenticationProvider =
        reauthenticationProvider.ThrowIfNull(nameof(reauthenticationProvider));
    private readonly IUnitOfWorkProvider _uowProvider = uowProvider.ThrowIfNull(
        nameof(uowProvider)
    );

    public Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request) =>
        RotateAsync(request, markOldKeyCompromised: false);

    public async Task<CompromiseAccountKeyResult> RotateCompromisedAccountKeyAsync(
        RotateCompromisedAccountKeyRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (await RefusedProofAsync(request.Proof) is { } refused)
            return refused;

        var uowSucceeded = false;
        try
        {
            await _uowProvider.BeginAsync();
            var result = await RotateAsync(
                new RotateAccountKeyRequest(request.AccountId, request.KeyType),
                markOldKeyCompromised: true
            );
            if (result.ResultCode != ModifyResultCode.Success)
            {
                return new CompromiseAccountKeyResult(
                    result.ResultCode == ModifyResultCode.NotFoundError
                        ? CompromiseAccountKeyResultCode.NotFoundError
                        : CompromiseAccountKeyResultCode.ValidationError,
                    result.Message
                );
            }

            await _uowProvider.CommitAsync();
            uowSucceeded = true;
            return new CompromiseAccountKeyResult(
                CompromiseAccountKeyResultCode.Success,
                string.Empty
            );
        }
        finally
        {
            if (!uowSucceeded)
            {
                await _uowProvider.RollbackAsync();
            }
        }
    }

    public async Task<CompromiseAccountKeyResult> MarkAccountKeyCompromisedAsync(
        MarkAccountKeyCompromisedRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        if (await RefusedProofAsync(request.Proof) is { } refused)
            return refused;

        if (!Enum.IsDefined(request.KeyType))
        {
            return new CompromiseAccountKeyResult(
                CompromiseAccountKeyResultCode.ValidationError,
                $"Unknown key type {request.KeyType}"
            );
        }

        var account = await _accountRepo.GetAsync(
            a => a.Id == request.AccountId,
            include: q => q.Include(a => a.SymmetricKeys).Include(a => a.AsymmetricKeys)
        );

        var result =
            request.KeyType == KeyType.SymmetricEncryption
                ? await MarkSymmetricVersionAsync(account, request.Version)
                : await MarkAsymmetricVersionAsync(account, request.KeyType, request.Version);

        _logger.LogInformation(
            "Marking version {Version} of the {KeyType} key of account {AccountId} compromised: {ResultCode}",
            request.Version,
            request.KeyType,
            request.AccountId,
            result.ResultCode
        );
        return result;
    }

    private async Task<CompromiseAccountKeyResult?> RefusedProofAsync(AccountKeyProof proof)
    {
        var userId =
            _userContextProvider.GetUserContext()?.User?.Id
            ?? throw new InvalidOperationException(
                "A non-system user context is required to mark a key compromised."
            );

        var result = await _reauthenticationProvider.VerifyAccountKeyProofAsync(userId, proof);
        return result.ResultCode == ReauthenticationResultCode.Success
            ? null
            : new CompromiseAccountKeyResult(
                result.ResultCode.ToCompromiseAccountKeyResultCode(),
                result.Message
            );
    }

    private async Task<CompromiseAccountKeyResult> MarkSymmetricVersionAsync(
        AccountEntity? account,
        int version
    )
    {
        var current = account?.SymmetricKeys.CurrentVersion(KeyUsedFor.Encryption);
        var target = account
            ?.SymmetricKeys.Versions(KeyUsedFor.Encryption)
            .FirstOrDefault(k => k.Version == version);
        if (Unmarkable(version, current?.Version, target?.CompromisedUtc, target != null) is { } no)
            return no;

        var key = await _accountSymmetricKeyRepo.GetAsync(k => k.Id == target!.Id);
        key!.CompromisedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _accountSymmetricKeyRepo.UpdateAsync(key);
        return new CompromiseAccountKeyResult(CompromiseAccountKeyResultCode.Success, string.Empty);
    }

    private async Task<CompromiseAccountKeyResult> MarkAsymmetricVersionAsync(
        AccountEntity? account,
        KeyType keyType,
        int version
    )
    {
        var keyUsedFor =
            keyType == KeyType.AsymmetricSignature ? KeyUsedFor.Signature : KeyUsedFor.Encryption;
        var current = account?.AsymmetricKeys.CurrentVersion(keyUsedFor);
        var target = account
            ?.AsymmetricKeys.Versions(keyUsedFor)
            .FirstOrDefault(k => k.Version == version);
        if (Unmarkable(version, current?.Version, target?.CompromisedUtc, target != null) is { } no)
            return no;

        var key = await _accountAsymmetricKeyRepo.GetAsync(k => k.Id == target!.Id);
        key!.CompromisedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _accountAsymmetricKeyRepo.UpdateAsync(key);
        return new CompromiseAccountKeyResult(CompromiseAccountKeyResultCode.Success, string.Empty);
    }

    private static CompromiseAccountKeyResult? Unmarkable(
        int version,
        int? currentVersion,
        DateTime? compromisedUtc,
        bool exists
    )
    {
        if (!exists)
        {
            return new CompromiseAccountKeyResult(
                CompromiseAccountKeyResultCode.NotFoundError,
                $"This account has no key version {version}"
            );
        }

        if (version == currentVersion)
        {
            return new CompromiseAccountKeyResult(
                CompromiseAccountKeyResultCode.CurrentVersionError,
                $"Version {version} is the current key. Rotate it and mark the old key compromised instead, or the account would have no usable key"
            );
        }

        return compromisedUtc is null
            ? null
            : new CompromiseAccountKeyResult(
                CompromiseAccountKeyResultCode.AlreadyCompromisedError,
                $"Version {version} is already marked compromised"
            );
    }

    private async Task<ModifyResult> RotateAsync(
        RotateAccountKeyRequest request,
        bool markOldKeyCompromised
    )
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
            if (
                markOldKeyCompromised
                && account.SymmetricKeys.CurrentVersion(key.KeyUsedFor) is { } oldKey
            )
            {
                var compromised = await _accountSymmetricKeyRepo.GetAsync(k => k.Id == oldKey.Id);
                compromised!.CompromisedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _accountSymmetricKeyRepo.UpdateAsync(compromised);
            }
            await _accountSymmetricKeyRepo.CreateAsync(key.ToAccountEntity(account.Id));
            version = key.Version;
        }
        else
        {
            var key = NewAsymmetricKey(request.KeyType);
            key.Version = account.AsymmetricKeys.NextVersion(key.KeyUsedFor);
            if (
                markOldKeyCompromised
                && account.AsymmetricKeys.CurrentVersion(key.KeyUsedFor) is { } oldKey
            )
            {
                var compromised = await _accountAsymmetricKeyRepo.GetAsync(k => k.Id == oldKey.Id);
                compromised!.CompromisedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _accountAsymmetricKeyRepo.UpdateAsync(compromised);
            }
            await _accountAsymmetricKeyRepo.CreateAsync(key.ToAccountEntity(account.Id));
            version = key.Version;
        }

        _logger.LogInformation(
            "Rotated {KeyType} key for account {AccountId} to version {Version}, old key marked compromised: {Compromised}",
            request.KeyType,
            account.Id,
            version,
            markOldKeyCompromised
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

using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Models.Extensions;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Processors;

internal class AccountKeyProcessor(
    IReadonlyRepo<AccountEntity> accountRepo,
    ISecurityProvider securityProvider,
    IReauthenticationProvider reauthenticationProvider,
    IUserContextProvider userContextProvider,
    ILogger<AccountKeyProcessor> logger
) : IAccountKeyProcessor
{
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly ISecurityProvider _securityProvider = securityProvider.ThrowIfNull(
        nameof(securityProvider)
    );
    private readonly IReauthenticationProvider _reauthenticationProvider =
        reauthenticationProvider.ThrowIfNull(nameof(reauthenticationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly ILogger<AccountKeyProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<GetResult<AccountJsonWebKeySet>> GetJsonWebKeySetAsync(Guid accountId)
    {
        var account = await _accountRepo.GetAsync(
            a => a.Id == accountId,
            include: q => q.Include(a => a.AsymmetricKeys)
        );
        if (account == null)
        {
            return new GetResult<AccountJsonWebKeySet>(
                RetrieveResultCode.NotFoundError,
                $"Account with Id {accountId} not found",
                null
            );
        }

        var signingKeys = account
            .AsymmetricKeys.Versions(KeyUsedFor.Signature)
            .OrderByDescending(k => k.Version);
        var encryptionKey = account.AsymmetricKeys.CurrentVersion(KeyUsedFor.Encryption);

        List<AccountJsonWebKey> keys = [];
        foreach (
            var key in encryptionKey is null ? signingKeys : signingKeys.Prepend(encryptionKey)
        )
        {
            if (key.ToAccountJsonWebKey(accountId) is { } jsonWebKey)
            {
                keys.Add(jsonWebKey);
                continue;
            }

            _logger.LogWarning(
                "Version {Version} of account {AccountId}'s {KeyUsedFor} key uses {ProviderName}, which has no standard JSON Web Key form; it is left out of the key set",
                key.Version,
                accountId,
                key.KeyUsedFor,
                key.ProviderName
            );
        }

        return new GetResult<AccountJsonWebKeySet>(
            RetrieveResultCode.Success,
            string.Empty,
            new AccountJsonWebKeySet(keys)
        );
    }

    public async Task<RevealAccountKeyResult> RevealKeyAsync(RevealAccountKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userId =
            _userContextProvider.GetUserContext()?.User?.Id
            ?? throw new InvalidOperationException(
                "A non-system user context is required to reveal an account key."
            );

        var password = await _reauthenticationProvider.VerifyCurrentPasswordAsync(
            userId,
            request.CurrentPassword
        );
        if (password.ResultCode != ReauthenticationResultCode.Success)
        {
            return new RevealAccountKeyResult(
                password.ResultCode.ToRevealAccountKeyResultCode(),
                password.Message
            );
        }

        var totp = await _reauthenticationProvider.VerifyTotpCodeAsync(userId, request.TotpCode);
        if (totp.ResultCode != ReauthenticationResultCode.Success)
        {
            return new RevealAccountKeyResult(
                totp.ResultCode.ToRevealAccountKeyResultCode(),
                totp.Message
            );
        }

        var account = await _accountRepo.GetAsync(
            a => a.Id == request.AccountId,
            include: q => q.Include(a => a.SymmetricKeys).Include(a => a.AsymmetricKeys)
        );

        var result = request.KeyType switch
        {
            KeyType.SymmetricEncryption
                when account?.SymmetricKeys.CurrentVersion(KeyUsedFor.Encryption) is { } key =>
                Revealed(key.EncryptedKey, key.ProviderName, key.Version),
            KeyType.AsymmetricEncryption
                when account?.AsymmetricKeys.CurrentVersion(KeyUsedFor.Encryption) is { } key =>
                Revealed(key.EncryptedPrivateKey, key.ProviderName, key.Version),
            KeyType.AsymmetricSignature
                when account?.AsymmetricKeys.CurrentVersion(KeyUsedFor.Signature) is { } key =>
                Revealed(key.EncryptedPrivateKey, key.ProviderName, key.Version),
            _ => new RevealAccountKeyResult(
                RevealAccountKeyResultCode.NotFoundError,
                $"No {request.KeyType} key found for account {request.AccountId}"
            ),
        };

        _logger.LogInformation(
            "User {UserId} requested the {KeyType} key of account {AccountId}: {ResultCode}",
            userId,
            request.KeyType,
            request.AccountId,
            result.ResultCode
        );
        return result;
    }

    private RevealAccountKeyResult Revealed(
        string encryptedKey,
        string providerName,
        int version
    ) =>
        new(
            RevealAccountKeyResultCode.Success,
            string.Empty,
            _securityProvider.DecryptWithSystemKey(encryptedKey),
            providerName,
            version
        );
}

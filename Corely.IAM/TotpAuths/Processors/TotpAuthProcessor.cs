using System.Security.Cryptography;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Constants;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Providers;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.TotpAuths.Processors;

internal class TotpAuthProcessor(
    IRepo<TotpAuthEntity> totpAuthRepo,
    IRepo<TotpRecoveryCodeEntity> recoveryCodeRepo,
    ITotpProvider totpProvider,
    ISecurityConfigurationProvider securityConfigProvider,
    ISymmetricEncryptionProviderFactory encryptionProviderFactory,
    IHashProviderFactory hashProviderFactory,
    TimeProvider timeProvider,
    ILogger<TotpAuthProcessor> logger
) : ITotpAuthProcessor
{
    private readonly IRepo<TotpAuthEntity> _totpAuthRepo = totpAuthRepo.ThrowIfNull(
        nameof(totpAuthRepo)
    );
    private readonly IRepo<TotpRecoveryCodeEntity> _recoveryCodeRepo = recoveryCodeRepo.ThrowIfNull(
        nameof(recoveryCodeRepo)
    );
    private readonly ITotpProvider _totpProvider = totpProvider.ThrowIfNull(nameof(totpProvider));
    private readonly ISecurityConfigurationProvider _securityConfigProvider =
        securityConfigProvider.ThrowIfNull(nameof(securityConfigProvider));
    private readonly ISymmetricEncryptionProviderFactory _encryptionProviderFactory =
        encryptionProviderFactory.ThrowIfNull(nameof(encryptionProviderFactory));
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly ILogger<TotpAuthProcessor> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<EnableTotpResult> EnableTotpAsync(
        Guid userId,
        string issuer,
        string userLabel
    )
    {
        var existing = await _totpAuthRepo.GetAsync(e => e.UserId == userId);
        if (existing is { IsEnabled: true })
        {
            return new EnableTotpResult(
                EnableTotpResultCode.AlreadyEnabledError,
                "TOTP is already enabled",
                null,
                null,
                null
            );
        }

        var secret = _totpProvider.GenerateSecret();
        var setupUri = _totpProvider.GenerateSetupUri(secret, issuer, userLabel);
        var encryptedSecret = EncryptWithSystemKey(secret);

        if (existing != null)
        {
            existing.EncryptedSecret = encryptedSecret;
            existing.IsEnabled = false;
            await _totpAuthRepo.UpdateAsync(existing);

            var oldCodes = await _recoveryCodeRepo.ListAsync(c => c.TotpAuthId == existing.Id);
            foreach (var code in oldCodes)
                await _recoveryCodeRepo.DeleteAsync(code);
        }
        else
        {
            existing = new TotpAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                EncryptedSecret = encryptedSecret,
                IsEnabled = false,
            };
            await _totpAuthRepo.CreateAsync(existing);
        }

        var recoveryCodes = await GenerateRecoveryCodesAsync(existing.Id);

        _logger.LogDebug("TOTP setup initiated for UserId {UserId}", userId);

        return new EnableTotpResult(
            EnableTotpResultCode.Success,
            string.Empty,
            secret,
            setupUri,
            recoveryCodes
        );
    }

    public async Task<ConfirmTotpResult> ConfirmTotpAsync(Guid userId, string code)
    {
        var totpAuth = await _totpAuthRepo.GetAsync(e => e.UserId == userId);
        if (totpAuth == null)
        {
            return new ConfirmTotpResult(
                ConfirmTotpResultCode.NotFoundError,
                "No TOTP setup found"
            );
        }

        if (totpAuth.IsEnabled)
        {
            return new ConfirmTotpResult(
                ConfirmTotpResultCode.AlreadyEnabledError,
                "TOTP is already enabled"
            );
        }

        var secret = DecryptWithSystemKey(totpAuth.EncryptedSecret);
        if (!_totpProvider.ValidateCode(secret, code))
        {
            return new ConfirmTotpResult(
                ConfirmTotpResultCode.InvalidCodeError,
                "Invalid TOTP code"
            );
        }

        totpAuth.IsEnabled = true;
        await _totpAuthRepo.UpdateAsync(totpAuth);

        _logger.LogDebug("TOTP confirmed and enabled for UserId {UserId}", userId);

        return new ConfirmTotpResult(ConfirmTotpResultCode.Success, string.Empty);
    }

    public async Task<DisableTotpResult> DisableTotpAsync(Guid userId, string code)
    {
        var totpAuth = await _totpAuthRepo.GetAsync(
            e => e.UserId == userId,
            include: q => q.Include(e => e.RecoveryCodes!)
        );
        if (totpAuth is not { IsEnabled: true })
        {
            return new DisableTotpResult(
                DisableTotpResultCode.NotFoundError,
                "TOTP is not enabled"
            );
        }

        var secret = DecryptWithSystemKey(totpAuth.EncryptedSecret);
        if (!_totpProvider.ValidateCode(secret, code))
        {
            return new DisableTotpResult(
                DisableTotpResultCode.InvalidCodeError,
                "Invalid TOTP code"
            );
        }

        totpAuth.RecoveryCodes?.Clear();
        await _totpAuthRepo.DeleteAsync(totpAuth);

        _logger.LogDebug("TOTP disabled for UserId {UserId}", userId);

        return new DisableTotpResult(DisableTotpResultCode.Success, string.Empty);
    }

    public async Task<TotpStatusResult> GetTotpStatusAsync(Guid userId)
    {
        var totpAuth = await _totpAuthRepo.GetAsync(
            e => e.UserId == userId,
            include: q => q.Include(e => e.RecoveryCodes!)
        );

        if (totpAuth == null)
        {
            return new TotpStatusResult(TotpStatusResultCode.Success, string.Empty, false, 0);
        }

        var remaining = totpAuth.RecoveryCodes?.Count(c => c.UsedUtc == null) ?? 0;
        return new TotpStatusResult(
            TotpStatusResultCode.Success,
            string.Empty,
            totpAuth.IsEnabled,
            remaining
        );
    }

    public async Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync(
        Guid userId
    )
    {
        var totpAuth = await _totpAuthRepo.GetAsync(
            e => e.UserId == userId,
            include: q => q.Include(e => e.RecoveryCodes!)
        );

        if (totpAuth == null)
        {
            return new RegenerateTotpRecoveryCodesResult(
                RegenerateTotpRecoveryCodesResultCode.NotFoundError,
                "No TOTP setup found",
                null
            );
        }

        if (!totpAuth.IsEnabled)
        {
            return new RegenerateTotpRecoveryCodesResult(
                RegenerateTotpRecoveryCodesResultCode.NotEnabledError,
                "TOTP is not enabled",
                null
            );
        }

        totpAuth.RecoveryCodes?.Clear();
        await _totpAuthRepo.UpdateAsync(totpAuth);

        var recoveryCodes = await GenerateRecoveryCodesAsync(totpAuth.Id);

        _logger.LogDebug("Recovery codes regenerated for UserId {UserId}", userId);

        return new RegenerateTotpRecoveryCodesResult(
            RegenerateTotpRecoveryCodesResultCode.Success,
            string.Empty,
            recoveryCodes
        );
    }

    public async Task<VerifyTotpOrRecoveryCodeResult> VerifyTotpOrRecoveryCodeAsync(
        VerifyTotpOrRecoveryCodeRequest request
    )
    {
        var totpAuth = await _totpAuthRepo.GetAsync(
            e => e.UserId == request.UserId,
            include: q => q.Include(e => e.RecoveryCodes!)
        );

        if (totpAuth is not { IsEnabled: true })
        {
            return new VerifyTotpOrRecoveryCodeResult(
                VerifyTotpOrRecoveryCodeResultCode.NotFoundError,
                "TOTP is not enabled"
            );
        }

        var secret = DecryptWithSystemKey(totpAuth.EncryptedSecret);
        if (_totpProvider.ValidateCode(secret, request.Code))
        {
            return new VerifyTotpOrRecoveryCodeResult(
                VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid,
                string.Empty
            );
        }

        var normalizedCode = request.Code.Replace("-", string.Empty).ToUpperInvariant();
        var unusedCodes = totpAuth.RecoveryCodes?.Where(c => c.UsedUtc == null).ToList() ?? [];

        foreach (var recoveryCode in unusedCodes)
        {
            var hashedValue = recoveryCode.CodeHash.ToHashedValue(_hashProviderFactory);
            if (hashedValue.Verify(normalizedCode))
            {
                recoveryCode.UsedUtc = _timeProvider.GetUtcNow().UtcDateTime;
                await _recoveryCodeRepo.UpdateAsync(recoveryCode);

                _logger.LogDebug("Recovery code used for UserId {UserId}", request.UserId);

                return new VerifyTotpOrRecoveryCodeResult(
                    VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid,
                    string.Empty
                );
            }
        }

        return new VerifyTotpOrRecoveryCodeResult(
            VerifyTotpOrRecoveryCodeResultCode.InvalidCodeError,
            "Invalid TOTP or recovery code"
        );
    }

    public async Task<bool> IsTotpEnabledAsync(Guid userId)
    {
        var totpAuth = await _totpAuthRepo.GetAsync(e => e.UserId == userId);
        return totpAuth is { IsEnabled: true };
    }

    private string EncryptWithSystemKey(string plaintext)
    {
        var systemKeyProvider = _securityConfigProvider.GetSystemSymmetricKey();
        return _encryptionProviderFactory
            .GetDefaultProvider()
            .Encrypt(plaintext, systemKeyProvider);
    }

    private string DecryptWithSystemKey(string encryptedValue)
    {
        var systemKeyProvider = _securityConfigProvider.GetSystemSymmetricKey();
        return _encryptionProviderFactory
            .GetDefaultProvider()
            .Decrypt(encryptedValue, systemKeyProvider);
    }

    private async Task<string[]> GenerateRecoveryCodesAsync(Guid totpAuthId)
    {
        var codes = new string[TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT];

        for (var i = 0; i < codes.Length; i++)
        {
            var rawCode = GenerateRecoveryCode();
            codes[i] = FormatRecoveryCode(rawCode);

            var hashedValue = rawCode.ToHashedValueFromPlainText(_hashProviderFactory);
            var entity = new TotpRecoveryCodeEntity
            {
                Id = Guid.CreateVersion7(),
                TotpAuthId = totpAuthId,
                CodeHash = hashedValue.Hash!,
            };
            await _recoveryCodeRepo.CreateAsync(entity);
        }

        return codes;
    }

    private static string GenerateRecoveryCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return RandomNumberGenerator.GetString(chars, TotpAuthConstants.RECOVERY_CODE_LENGTH);
    }

    private static string FormatRecoveryCode(string code) => $"{code[..4]}-{code[4..]}";
}

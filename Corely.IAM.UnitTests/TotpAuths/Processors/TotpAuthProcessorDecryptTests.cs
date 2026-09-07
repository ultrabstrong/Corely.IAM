using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.TotpAuths.Providers;
using Corely.Security.Encryption;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.TotpAuths.Processors;

/// <summary>
/// A TOTP secret stored by an older default must stay verifiable after the default changes. The
/// encrypt/decrypt pair is private, so this drives it through the public verify path.
/// </summary>
public class TotpAuthProcessorDecryptTests
{
    private const string NON_DEFAULT_PROVIDER = SymmetricEncryptionConstants.AES_CODE;
    private const string CURRENT_DEFAULT_PROVIDER = SymmetricEncryptionConstants.AES_GCM_CODE;

    private readonly ServiceFactory _serviceFactory = new();
    private readonly TotpAuthProcessor _processor;
    private readonly ITotpProvider _totpProvider;
    private readonly ISecurityConfigurationProvider _securityConfig;

    public TotpAuthProcessorDecryptTests()
    {
        _totpProvider = _serviceFactory.GetRequiredService<ITotpProvider>();
        _securityConfig = _serviceFactory.GetRequiredService<ISecurityConfigurationProvider>();

        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(x => x.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        _processor = new TotpAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<TotpAuthEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<TotpRecoveryCodeEntity>>(),
            _totpProvider,
            _securityConfig,
            new SymmetricEncryptionProviderFactory(CURRENT_DEFAULT_PROVIDER),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            timeProvider.Object,
            _serviceFactory.GetRequiredService<ILogger<TotpAuthProcessor>>()
        );
    }

    [Fact]
    public async Task VerifyTotp_ReadsASecretWrittenByAnOlderDefault()
    {
        var userId = Guid.CreateVersion7();
        var secret = _totpProvider.GenerateSecret();

        // Stored when the default was something else - the shape every 1.x database is in.
        var encryptedSecret = new SymmetricEncryptionProviderFactory(NON_DEFAULT_PROVIDER)
            .GetDefaultProvider()
            .Encrypt(secret, _securityConfig.GetSystemSymmetricKey());

        Assert.StartsWith(NON_DEFAULT_PROVIDER, encryptedSecret);

        var totpAuthRepo = _serviceFactory.GetRequiredService<IRepo<TotpAuthEntity>>();
        await totpAuthRepo.CreateAsync(
            new TotpAuthEntity
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                EncryptedSecret = encryptedSecret,
                IsEnabled = true,
                RecoveryCodes = [],
            }
        );

        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, _totpProvider.GenerateCode(secret))
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid, result.ResultCode);
    }
}

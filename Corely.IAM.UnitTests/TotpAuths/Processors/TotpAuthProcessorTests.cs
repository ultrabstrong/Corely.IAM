using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Constants;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.TotpAuths.Providers;
using Corely.Security.Encryption.Factories;
using Corely.Security.Hashing.Factories;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.UnitTests.TotpAuths.Processors;

public class TotpAuthProcessorTests
{
    private readonly ServiceFactory _serviceFactory = new();
    private readonly TotpAuthProcessor _processor;
    private readonly ITotpProvider _totpProvider;

    public TotpAuthProcessorTests()
    {
        _totpProvider = _serviceFactory.GetRequiredService<ITotpProvider>();
        _processor = new TotpAuthProcessor(
            _serviceFactory.GetRequiredService<IRepo<TotpAuthEntity>>(),
            _serviceFactory.GetRequiredService<IRepo<TotpRecoveryCodeEntity>>(),
            _totpProvider,
            _serviceFactory.GetRequiredService<ISecurityConfigurationProvider>(),
            _serviceFactory.GetRequiredService<ISymmetricEncryptionProviderFactory>(),
            _serviceFactory.GetRequiredService<IHashProviderFactory>(),
            _serviceFactory.GetRequiredService<ILogger<TotpAuthProcessor>>()
        );
    }

    private async Task<string> EnableAndConfirmTotpAsync(Guid userId)
    {
        var enableResult = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");
        var code = _totpProvider.GenerateCode(enableResult.Secret!);
        await _processor.ConfirmTotpAsync(userId, code);
        await WireRecoveryCodesNavPropertyAsync(userId);
        return enableResult.Secret!;
    }

    // Mock repos don't support EF Core Include — manually wire the navigation property
    private async Task WireRecoveryCodesNavPropertyAsync(Guid userId)
    {
        var totpAuthRepo = _serviceFactory.GetRequiredService<IRepo<TotpAuthEntity>>();
        var recoveryCodeRepo = _serviceFactory.GetRequiredService<IRepo<TotpRecoveryCodeEntity>>();
        var totpAuth = await totpAuthRepo.GetAsync(e => e.UserId == userId);
        if (totpAuth != null)
        {
            var codes = await recoveryCodeRepo.ListAsync(c => c.TotpAuthId == totpAuth.Id);
            totpAuth.RecoveryCodes = codes.ToList();
        }
    }

    [Fact]
    public async Task EnableTotpAsync_ReturnsSuccess_WhenNoExistingTotpAuth()
    {
        var userId = Guid.CreateVersion7();

        var result = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        Assert.Equal(EnableTotpResultCode.Success, result.ResultCode);
        Assert.NotNull(result.Secret);
        Assert.NotNull(result.SetupUri);
        Assert.NotNull(result.RecoveryCodes);
        Assert.Equal(TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT, result.RecoveryCodes.Length);
    }

    [Fact]
    public async Task EnableTotpAsync_ReturnsAlreadyEnabledError_WhenTotpIsAlreadyEnabled()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        var result = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        Assert.Equal(EnableTotpResultCode.AlreadyEnabledError, result.ResultCode);
        Assert.Null(result.Secret);
    }

    [Fact]
    public async Task EnableTotpAsync_UpdatesExistingEntity_WhenNotYetEnabled()
    {
        var userId = Guid.CreateVersion7();
        await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        var secondResult = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        Assert.Equal(EnableTotpResultCode.Success, secondResult.ResultCode);
        Assert.NotNull(secondResult.Secret);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsSuccess_WhenCodeIsValid()
    {
        var userId = Guid.CreateVersion7();
        var enableResult = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");
        var validCode = _totpProvider.GenerateCode(enableResult.Secret!);

        var result = await _processor.ConfirmTotpAsync(userId, validCode);

        Assert.Equal(ConfirmTotpResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsNotFoundError_WhenNoTotpSetup()
    {
        var result = await _processor.ConfirmTotpAsync(Guid.CreateVersion7(), "123456");

        Assert.Equal(ConfirmTotpResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsAlreadyEnabledError_WhenAlreadyEnabled()
    {
        var userId = Guid.CreateVersion7();
        var secret = await EnableAndConfirmTotpAsync(userId);
        var code = _totpProvider.GenerateCode(secret);

        var result = await _processor.ConfirmTotpAsync(userId, code);

        Assert.Equal(ConfirmTotpResultCode.AlreadyEnabledError, result.ResultCode);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsInvalidCodeError_WhenCodeIsInvalid()
    {
        var userId = Guid.CreateVersion7();
        await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        var result = await _processor.ConfirmTotpAsync(userId, "000000");

        Assert.Equal(ConfirmTotpResultCode.InvalidCodeError, result.ResultCode);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsSuccess_WhenCodeIsValid()
    {
        var userId = Guid.CreateVersion7();
        var secret = await EnableAndConfirmTotpAsync(userId);
        var validCode = _totpProvider.GenerateCode(secret);

        var result = await _processor.DisableTotpAsync(userId, validCode);

        Assert.Equal(DisableTotpResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsNotFoundError_WhenTotpNotEnabled()
    {
        var result = await _processor.DisableTotpAsync(Guid.CreateVersion7(), "123456");

        Assert.Equal(DisableTotpResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsNotFoundError_WhenTotpExistsButNotEnabled()
    {
        var userId = Guid.CreateVersion7();
        await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        var result = await _processor.DisableTotpAsync(userId, "123456");

        Assert.Equal(DisableTotpResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsInvalidCodeError_WhenCodeIsInvalid()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        var result = await _processor.DisableTotpAsync(userId, "000000");

        Assert.Equal(DisableTotpResultCode.InvalidCodeError, result.ResultCode);
    }

    [Fact]
    public async Task GetTotpStatusAsync_ReturnsNotEnabled_WhenNoTotpAuthExists()
    {
        var result = await _processor.GetTotpStatusAsync(Guid.CreateVersion7());

        Assert.Equal(TotpStatusResultCode.Success, result.ResultCode);
        Assert.False(result.IsEnabled);
        Assert.Equal(0, result.RemainingRecoveryCodes);
    }

    [Fact]
    public async Task GetTotpStatusAsync_ReturnsEnabled_WithCorrectRecoveryCodeCount()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        var result = await _processor.GetTotpStatusAsync(userId);

        Assert.Equal(TotpStatusResultCode.Success, result.ResultCode);
        Assert.True(result.IsEnabled);
        Assert.Equal(TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT, result.RemainingRecoveryCodes);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_ReturnsSuccess_WhenTotpIsEnabled()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        var result = await _processor.RegenerateTotpRecoveryCodesAsync(userId);

        Assert.Equal(RegenerateTotpRecoveryCodesResultCode.Success, result.ResultCode);
        Assert.NotNull(result.RecoveryCodes);
        Assert.Equal(TotpAuthConstants.DEFAULT_RECOVERY_CODE_COUNT, result.RecoveryCodes.Length);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_ReturnsNotFoundError_WhenNoTotpSetup()
    {
        var result = await _processor.RegenerateTotpRecoveryCodesAsync(Guid.CreateVersion7());

        Assert.Equal(RegenerateTotpRecoveryCodesResultCode.NotFoundError, result.ResultCode);
        Assert.Null(result.RecoveryCodes);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_ReturnsNotEnabledError_WhenNotEnabled()
    {
        var userId = Guid.CreateVersion7();
        await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        var result = await _processor.RegenerateTotpRecoveryCodesAsync(userId);

        Assert.Equal(RegenerateTotpRecoveryCodesResultCode.NotEnabledError, result.ResultCode);
        Assert.Null(result.RecoveryCodes);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_ReturnsTotpCodeValid_WhenTotpCodeIsCorrect()
    {
        var userId = Guid.CreateVersion7();
        var secret = await EnableAndConfirmTotpAsync(userId);
        var validCode = _totpProvider.GenerateCode(secret);

        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, validCode)
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_ReturnsRecoveryCodeValid_WhenRecoveryCodeMatches()
    {
        var userId = Guid.CreateVersion7();
        var enableResult = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");
        var recoveryCode = enableResult.RecoveryCodes![0];
        var confirmCode = _totpProvider.GenerateCode(enableResult.Secret!);
        await _processor.ConfirmTotpAsync(userId, confirmCode);
        await WireRecoveryCodesNavPropertyAsync(userId);

        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, recoveryCode)
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_ReturnsInvalidCodeError_WhenBothFail()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, "XXXX-XXXX")
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.InvalidCodeError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_ReturnsNotFoundError_WhenTotpNotEnabled()
    {
        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(Guid.CreateVersion7(), "123456")
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.NotFoundError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_SkipsUsedRecoveryCodes()
    {
        var userId = Guid.CreateVersion7();
        var enableResult = await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");
        var recoveryCode = enableResult.RecoveryCodes![0];
        var confirmCode = _totpProvider.GenerateCode(enableResult.Secret!);
        await _processor.ConfirmTotpAsync(userId, confirmCode);
        await WireRecoveryCodesNavPropertyAsync(userId);

        await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, recoveryCode)
        );

        var result = await _processor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(userId, recoveryCode)
        );

        Assert.Equal(VerifyTotpOrRecoveryCodeResultCode.InvalidCodeError, result.ResultCode);
    }

    [Fact]
    public async Task IsTotpEnabledAsync_ReturnsTrue_WhenEnabled()
    {
        var userId = Guid.CreateVersion7();
        await EnableAndConfirmTotpAsync(userId);

        Assert.True(await _processor.IsTotpEnabledAsync(userId));
    }

    [Fact]
    public async Task IsTotpEnabledAsync_ReturnsFalse_WhenNotEnabled()
    {
        var userId = Guid.CreateVersion7();
        await _processor.EnableTotpAsync(userId, "TestIssuer", "user@test.com");

        Assert.False(await _processor.IsTotpEnabledAsync(userId));
    }

    [Fact]
    public async Task IsTotpEnabledAsync_ReturnsFalse_WhenNoTotpAuthExists()
    {
        Assert.False(await _processor.IsTotpEnabledAsync(Guid.CreateVersion7()));
    }
}

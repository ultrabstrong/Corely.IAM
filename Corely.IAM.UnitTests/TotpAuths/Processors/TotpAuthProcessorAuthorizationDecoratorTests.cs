using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;

namespace Corely.IAM.UnitTests.TotpAuths.Processors;

public class TotpAuthProcessorAuthorizationDecoratorTests
{
    private readonly Mock<ITotpAuthProcessor> _mockInnerProcessor = new();
    private readonly Mock<IAuthorizationProvider> _mockAuthorizationProvider = new();
    private readonly TotpAuthProcessorAuthorizationDecorator _decorator;

    public TotpAuthProcessorAuthorizationDecoratorTests()
    {
        _decorator = new TotpAuthProcessorAuthorizationDecorator(
            _mockInnerProcessor.Object,
            _mockAuthorizationProvider.Object
        );
    }

    [Fact]
    public async Task EnableTotpAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.EnableTotpAsync(userId, "issuer", "label");

        Assert.Equal(EnableTotpResultCode.UnauthorizedError, result.ResultCode);
        _mockInnerProcessor.Verify(
            x => x.EnableTotpAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task EnableTotpAsync_Delegates_WhenAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        var expected = new EnableTotpResult(
            EnableTotpResultCode.Success,
            string.Empty,
            "secret",
            "setup-uri",
            ["ABCD-EFGH"]
        );
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(true);
        _mockInnerProcessor
            .Setup(x => x.EnableTotpAsync(userId, "issuer", "label"))
            .ReturnsAsync(expected);

        var result = await _decorator.EnableTotpAsync(userId, "issuer", "label");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ConfirmTotpAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.ConfirmTotpAsync(userId, "123456");

        Assert.Equal(ConfirmTotpResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task DisableTotpAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.DisableTotpAsync(userId, "123456");

        Assert.Equal(DisableTotpResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task GetTotpStatusAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.GetTotpStatusAsync(userId);

        Assert.Equal(TotpStatusResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task RegenerateTotpRecoveryCodesAsync_ReturnsUnauthorized_WhenNotAuthorizedForOwnUser()
    {
        var userId = Guid.CreateVersion7();
        _mockAuthorizationProvider
            .Setup(x => x.IsAuthorizedForOwnUser(userId, It.IsAny<bool>()))
            .Returns(false);

        var result = await _decorator.RegenerateTotpRecoveryCodesAsync(userId);

        Assert.Equal(RegenerateTotpRecoveryCodesResultCode.UnauthorizedError, result.ResultCode);
    }

    [Fact]
    public async Task VerifyTotpOrRecoveryCodeAsync_BypassesAuthorization_AndDelegates()
    {
        var request = new VerifyTotpOrRecoveryCodeRequest(Guid.CreateVersion7(), "123456");
        var expected = new VerifyTotpOrRecoveryCodeResult(
            VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid,
            string.Empty
        );
        _mockInnerProcessor
            .Setup(x => x.VerifyTotpOrRecoveryCodeAsync(request))
            .ReturnsAsync(expected);

        var result = await _decorator.VerifyTotpOrRecoveryCodeAsync(request);

        Assert.Equal(expected, result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public async Task IsTotpEnabledAsync_BypassesAuthorization_AndDelegates()
    {
        var userId = Guid.CreateVersion7();
        _mockInnerProcessor.Setup(x => x.IsTotpEnabledAsync(userId)).ReturnsAsync(true);

        var result = await _decorator.IsTotpEnabledAsync(userId);

        Assert.True(result);
        _mockAuthorizationProvider.Verify(
            x => x.IsAuthorizedForOwnUser(It.IsAny<Guid>(), It.IsAny<bool>()),
            Times.Never
        );
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerProcessor() =>
        Assert.Throws<ArgumentNullException>(() =>
            new TotpAuthProcessorAuthorizationDecorator(null!, _mockAuthorizationProvider.Object)
        );

    [Fact]
    public void Constructor_ThrowsOnNullAuthorizationProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new TotpAuthProcessorAuthorizationDecorator(_mockInnerProcessor.Object, null!)
        );
}

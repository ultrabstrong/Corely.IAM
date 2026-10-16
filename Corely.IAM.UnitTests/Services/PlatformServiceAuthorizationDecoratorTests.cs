using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Services;

namespace Corely.IAM.UnitTests.Services;

public class PlatformServiceAuthorizationDecoratorTests
{
    private readonly Mock<IPlatformService> _inner = new();
    private readonly Mock<IAuditAccessProvider> _access = new();
    private readonly PlatformServiceAuthorizationDecorator _decorator;

    public PlatformServiceAuthorizationDecoratorTests()
    {
        _decorator = new PlatformServiceAuthorizationDecorator(_inner.Object, _access.Object);
    }

    private void Holds(AuthAction action, bool holds) =>
        _access
            .Setup(a => a.HoldsInPlatformAccountAsync(action, SecurityPolicy.RESOURCE_TYPE))
            .ReturnsAsync(holds);

    [Fact]
    public async Task GetSecuritySettings_DelegatesToInner_WhenReadIsHeldInThePlatformAccount()
    {
        Holds(AuthAction.Read, true);
        var expected = new GetSecuritySettingsResult(
            RetrieveResultCode.Success,
            "",
            SecurityPolicy.Default
        );
        _inner.Setup(s => s.GetSecuritySettingsAsync()).ReturnsAsync(expected);

        Assert.Equal(expected, await _decorator.GetSecuritySettingsAsync());
    }

    [Fact]
    public async Task GetSecuritySettings_IsRefused_WithoutRead()
    {
        Holds(AuthAction.Read, false);

        var result = await _decorator.GetSecuritySettingsAsync();

        Assert.Equal(RetrieveResultCode.UnauthorizedError, result.ResultCode);
        _inner.Verify(s => s.GetSecuritySettingsAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateSecuritySettings_DelegatesToInner_WhenUpdateIsHeldInThePlatformAccount()
    {
        Holds(AuthAction.Update, true);
        _inner
            .Setup(s => s.UpdateSecuritySettingsAsync(SecurityPolicy.Default))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));

        var result = await _decorator.UpdateSecuritySettingsAsync(SecurityPolicy.Default);

        Assert.Equal(ModifyResultCode.Success, result.ResultCode);
    }

    [Fact]
    public async Task UpdateSecuritySettings_IsRefused_WithoutUpdate()
    {
        Holds(AuthAction.Update, false);

        var result = await _decorator.UpdateSecuritySettingsAsync(SecurityPolicy.Default);

        Assert.Equal(ModifyResultCode.UnauthorizedError, result.ResultCode);
        _inner.Verify(s => s.UpdateSecuritySettingsAsync(It.IsAny<SecurityPolicy>()), Times.Never);
    }

    [Fact]
    public async Task SecuritySettings_AreRefused_ToAHolderOfOnlyTheAuditSettingsType()
    {
        _access
            .Setup(a =>
                a.HoldsInPlatformAccountAsync(
                    It.IsAny<AuthAction>(),
                    PlatformSettings.RESOURCE_TYPE
                )
            )
            .ReturnsAsync(true);

        var read = await _decorator.GetSecuritySettingsAsync();
        var update = await _decorator.UpdateSecuritySettingsAsync(SecurityPolicy.Default);

        Assert.Equal(RetrieveResultCode.UnauthorizedError, read.ResultCode);
        Assert.Equal(ModifyResultCode.UnauthorizedError, update.ResultCode);
    }

    [Fact]
    public async Task Bootstrap_PassesThrough()
    {
        var request = new BootstrapPlatformRequest("Platform", "owner", "owner@example.com");

        await _decorator.BootstrapPlatformAsync(request);

        _inner.Verify(s => s.BootstrapPlatformAsync(request), Times.Once);
        _access.VerifyNoOtherCalls();
    }
}

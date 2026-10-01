using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Web.Components.Shared;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class EncryptionSigningPanelTests : TestContext
{
    private readonly Mock<IIamSymmetricEncryptionProvider> _symProvider = new();

    public EncryptionSigningPanelTests()
    {
        _symProvider.Setup(x => x.ProviderName).Returns("AES");
        _symProvider.Setup(x => x.ProviderDescription).Returns("AES test");
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Panel_ShowsTheCurrentKeyVersion()
    {
        _symProvider.Setup(x => x.Version).Returns(3);

        var cut = Render<EncryptionSigningPanel>(p =>
            p.Add(x => x.SymProvider, _symProvider.Object)
        );

        Assert.Contains("Version 3", cut.Markup);
    }

    [Fact]
    public void Panel_HidesRotate_WithoutRotateCallback()
    {
        var cut = Render<EncryptionSigningPanel>(p =>
            p.Add(x => x.SymProvider, _symProvider.Object)
        );

        Assert.DoesNotContain("Rotate key", cut.Markup);
    }

    [Fact]
    public void Panel_RotatesTheShownKey_AfterConfirm()
    {
        KeyType? rotated = null;
        var cut = Render<EncryptionSigningPanel>(p =>
            p.Add(x => x.SymProvider, _symProvider.Object)
                .Add(
                    x => x.RotateKeyAsync,
                    keyType =>
                    {
                        rotated = keyType;
                        return Task.FromResult(
                            new ModifyResult(ModifyResultCode.Success, string.Empty)
                        );
                    }
                )
        );

        cut.FindAll("button").Single(b => b.TextContent == "Rotate key").Click();
        Assert.Contains("Values encrypted before still decrypt", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Rotate").Click();

        Assert.Equal(KeyType.SymmetricEncryption, rotated);
        Assert.Contains("Key rotated.", cut.Markup);
    }

    [Fact]
    public void Panel_ShowsTheFailure_WhenRotationFails()
    {
        var cut = Render<EncryptionSigningPanel>(p =>
            p.Add(x => x.SymProvider, _symProvider.Object)
                .Add(
                    x => x.RotateKeyAsync,
                    _ =>
                        Task.FromResult(
                            new ModifyResult(ModifyResultCode.UnauthorizedError, "not allowed")
                        )
                )
        );

        cut.FindAll("button").Single(b => b.TextContent == "Rotate key").Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Rotate").Click();

        Assert.Contains("Rotation failed: not allowed", cut.Markup);
    }
}

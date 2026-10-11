using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class PasswordSectionTests : TestContext
{
    private const string CURRENT_PLACEHOLDER = "placeholder=\"Current password\"";
    private const string CODE_PLACEHOLDER = "placeholder=\"Authenticator code\"";

    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<IModificationService> _modificationService = new();
    private readonly Mock<IDeregistrationService> _deregistrationService = new();
    private readonly Mock<IMfaService> _mfaService = new();

    public PasswordSectionTests()
    {
        Services.AddSingleton(_registrationService.Object);
        Services.AddSingleton(_modificationService.Object);
        Services.AddSingleton(_deregistrationService.Object);
        Services.AddSingleton(_mfaService.Object);
        Services.AddSingleton(new Mock<IBlazorUserContextAccessor>().Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetTotpEnabled(false);
    }

    private void SetTotpEnabled(bool enabled) =>
        _mfaService
            .Setup(x => x.GetTotpStatusAsync())
            .ReturnsAsync(new TotpStatusResult(TotpStatusResultCode.Success, "", enabled, 0));

    private IRenderedComponent<PasswordSection> RenderSection(
        bool hasBasicAuth,
        bool hasGoogleAuth = false
    ) =>
        Render<PasswordSection>(p =>
            p.Add(x => x.HasBasicAuth, hasBasicAuth).Add(x => x.HasGoogleAuth, hasGoogleAuth)
        );

    private static void Click(IRenderedComponent<PasswordSection> cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text).Click();

    private static void FillPassword(
        IRenderedComponent<PasswordSection> cut,
        string placeholder,
        string value
    ) => cut.Find($"input[placeholder='{placeholder}']").Change(value);

    [Fact]
    public void ChangeForm_AsksForCurrentPasswordAndCode_WhenTotpIsEnabled()
    {
        SetTotpEnabled(true);
        var cut = RenderSection(hasBasicAuth: true);

        Click(cut, "Change Password");

        Assert.Contains(CURRENT_PLACEHOLDER, cut.Markup);
        Assert.Contains(CODE_PLACEHOLDER, cut.Markup);
    }

    [Fact]
    public void ChangeForm_LeavesOutTheCode_WhenTotpIsNotEnabled()
    {
        var cut = RenderSection(hasBasicAuth: true);

        Click(cut, "Change Password");

        Assert.Contains(CURRENT_PLACEHOLDER, cut.Markup);
        Assert.DoesNotContain(CODE_PLACEHOLDER, cut.Markup);
    }

    [Fact]
    public void ChangeForm_ModifiesThePassword_WithTheCurrentPasswordAndCode()
    {
        SetTotpEnabled(true);
        _modificationService
            .Setup(x => x.ModifyPasswordAsync(It.IsAny<ModifyPasswordRequest>()))
            .ReturnsAsync(new ModifyPasswordResult(ModifyPasswordResultCode.Success, ""));
        var cut = RenderSection(hasBasicAuth: true);
        Click(cut, "Change Password");

        FillPassword(cut, "Current password", "old");
        FillPassword(cut, "New password", "NewPassword1!");
        FillPassword(cut, "Confirm password", "NewPassword1!");
        cut.Find("input[placeholder='Authenticator code']").Change("123456");
        Click(cut, "Save");

        _modificationService.Verify(
            x => x.ModifyPasswordAsync(new ModifyPasswordRequest("old", "NewPassword1!", "123456")),
            Times.Once
        );
        _registrationService.VerifyNoOtherCalls();
        Assert.Contains("Password changed successfully.", cut.Markup);
    }

    [Fact]
    public void ChangeForm_ShowsTheFailureMessage()
    {
        _modificationService
            .Setup(x => x.ModifyPasswordAsync(It.IsAny<ModifyPasswordRequest>()))
            .ReturnsAsync(
                new ModifyPasswordResult(
                    ModifyPasswordResultCode.InvalidCurrentPasswordError,
                    "Current password is incorrect"
                )
            );
        var cut = RenderSection(hasBasicAuth: true);
        Click(cut, "Change Password");

        FillPassword(cut, "Current password", "wrong");
        FillPassword(cut, "New password", "NewPassword1!");
        FillPassword(cut, "Confirm password", "NewPassword1!");
        Click(cut, "Save");

        Assert.Contains("Current password is incorrect", cut.Markup);
    }

    [Fact]
    public void NoPassword_RegistersAPassword_WithoutAskingForTheCurrentOne()
    {
        _registrationService
            .Setup(x => x.RegisterPasswordAsync(It.IsAny<RegisterPasswordRequest>()))
            .ReturnsAsync(new RegisterPasswordResult(RegisterPasswordResultCode.Success, ""));
        var cut = RenderSection(hasBasicAuth: false, hasGoogleAuth: true);

        Assert.DoesNotContain(CURRENT_PLACEHOLDER, cut.Markup);
        FillPassword(cut, "New password", "NewPassword1!");
        FillPassword(cut, "Confirm password", "NewPassword1!");
        Click(cut, "Set Password");

        _registrationService.Verify(
            x => x.RegisterPasswordAsync(new RegisterPasswordRequest("NewPassword1!", null)),
            Times.Once
        );
        _modificationService.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoPassword_AsksForTheCode_WhenTotpIsEnabled()
    {
        SetTotpEnabled(true);

        var cut = RenderSection(hasBasicAuth: false, hasGoogleAuth: true);

        Assert.Contains(CODE_PLACEHOLDER, cut.Markup);
    }

    [Fact]
    public void Remove_IsOfferedOnlyWithGoogleSignIn()
    {
        Assert.DoesNotContain("Remove Password", RenderSection(hasBasicAuth: true).Markup);
        Assert.Contains(
            "Remove Password",
            RenderSection(hasBasicAuth: true, hasGoogleAuth: true).Markup
        );
    }

    [Fact]
    public void Remove_DeregistersWithTheCurrentPassword()
    {
        _deregistrationService
            .Setup(x => x.DeregisterBasicAuthAsync(It.IsAny<DeregisterBasicAuthRequest>()))
            .ReturnsAsync(new DeregisterBasicAuthResult(DeregisterBasicAuthResultCode.Success, ""));
        var cut = RenderSection(hasBasicAuth: true, hasGoogleAuth: true);
        Click(cut, "Remove Password");

        FillPassword(cut, "Current password", "old");
        Click(cut, "Confirm Remove");

        _deregistrationService.Verify(
            x => x.DeregisterBasicAuthAsync(new DeregisterBasicAuthRequest("old", null)),
            Times.Once
        );
        Assert.Contains("Password removed.", cut.Markup);
    }
}

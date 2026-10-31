using AngleSharp.Dom;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class EncryptionSigningPanelKeyCopyTests : BunitContext
{
    private const string SECRET = "dGhlLXNlY3JldC1rZXk=";

    private readonly ManualTimeProvider _time = new();
    private readonly List<(KeyType KeyType, string? Password, string? TotpCode)> _calls = [];
    private RevealAccountKeyResult _result = new(
        RevealAccountKeyResultCode.Success,
        string.Empty,
        SECRET,
        "AES-256-GCM",
        3
    );

    public EncryptionSigningPanelKeyCopyTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<TimeProvider>(_time);
    }

    private IRenderedComponent<EncryptionSigningPanel> RenderPanel(bool reveal = true) =>
        Render<EncryptionSigningPanel>(p =>
        {
            p.Add(
                x => x.RotateKeyAsync,
                _ => Task.FromResult(new ModifyResult(ModifyResultCode.Success, string.Empty))
            );
            if (reveal)
                p.Add(
                    x => x.RevealKeyAsync,
                    (keyType, password, totpCode) =>
                    {
                        _calls.Add((keyType, password, totpCode));
                        return Task.FromResult(_result);
                    }
                );
        });

    private static IElement Button(IRenderedComponent<EncryptionSigningPanel> cut, string text) =>
        cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

    private static void ShowTab(IRenderedComponent<EncryptionSigningPanel> cut, int tab) =>
        cut.FindAll(".nav-link")[tab].Click();

    private static void Reveal(
        IRenderedComponent<EncryptionSigningPanel> cut,
        string label,
        string password = "pw"
    )
    {
        Button(cut, label).Click();
        cut.Find(".modal-body input[type=password]").Change(password);
        Button(cut, "Show key").Click();
    }

    [Theory]
    [InlineData(0, "Copy key")]
    [InlineData(1, "Copy private key")]
    [InlineData(2, "Copy private key")]
    public void CopyAction_SitsImmediatelyLeftOfRotate_OnEveryTab(int tab, string label)
    {
        var cut = RenderPanel();
        ShowTab(cut, tab);

        var buttons = cut.FindAll(".enc-actions button").Select(b => b.TextContent.Trim()).ToList();

        Assert.Equal(buttons.IndexOf("Rotate key") - 1, buttons.IndexOf(label));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CopyAction_IsAbsent_WithoutRevealCallback(int tab)
    {
        var cut = RenderPanel(reveal: false);
        ShowTab(cut, tab);

        Assert.DoesNotContain("Copy key", cut.Markup);
        Assert.DoesNotContain("Copy private key", cut.Markup);
    }

    [Theory]
    [InlineData(0, "Copy key", KeyType.SymmetricEncryption)]
    [InlineData(1, "Copy private key", KeyType.AsymmetricEncryption)]
    [InlineData(2, "Copy private key", KeyType.AsymmetricSignature)]
    public void Reveal_WarnsBeforeTheKeyIsShown_AndKeepsTheWarningBesideIt(
        int tab,
        string label,
        KeyType keyType
    )
    {
        var cut = RenderPanel();
        ShowTab(cut, tab);

        Button(cut, label).Click();

        var warnings = keyType.CopyKeyWarnings();
        foreach (var warning in warnings)
            Assert.Contains(warning, cut.Find(".modal-body").TextContent);
        Assert.DoesNotContain(SECRET, cut.Markup);
        Assert.Empty(_calls);

        cut.Find(".modal-body input[type=password]").Change("pw");
        cut.Find(".modal-body input[type=text]").Change("123456");
        Button(cut, "Show key").Click();

        Assert.Equal((keyType, "pw", "123456"), Assert.Single(_calls));
        Assert.Equal(SECRET, cut.Find(".tab-content textarea[readonly]").TextContent);
        foreach (var warning in warnings)
            Assert.Contains(warning, cut.Find(".tab-content .alert-warning").TextContent);
        Assert.Contains("version 3", cut.Markup);
    }

    [Fact]
    public void Reveal_ShowsTheRefusal_AndNoKey_ForAFailedReauthentication()
    {
        _result = new RevealAccountKeyResult(
            RevealAccountKeyResultCode.InvalidCurrentPasswordError,
            "Current password is incorrect"
        );
        var cut = RenderPanel();

        Reveal(cut, "Copy key");

        Assert.Contains("Current password is incorrect", cut.Find(".alert-danger").TextContent);
        Assert.DoesNotContain(SECRET, cut.Markup);
    }

    [Fact]
    public void Reveal_ForgetsThePassword_OnceItIsSent()
    {
        var cut = RenderPanel();
        Reveal(cut, "Copy key", "first-password");
        Button(cut, "Copy key").Click();

        Assert.Empty(cut.Find(".modal-body input[type=password]").GetAttribute("value") ?? "");
    }

    [Fact]
    public void Copy_WritesTheKeyToTheClipboard()
    {
        var cut = RenderPanel();
        Reveal(cut, "Copy key");

        cut.Find("button[title='Copy']").Click();

        JSInterop.VerifyInvoke("navigator.clipboard.writeText");
        Assert.Equal(
            SECRET,
            JSInterop.Invocations["navigator.clipboard.writeText"][0].Arguments[0]
        );
    }

    [Fact]
    public void Hide_ClearsTheKey()
    {
        var cut = RenderPanel();
        Reveal(cut, "Copy key");

        cut.Find("button[title='Hide']").Click();

        Assert.DoesNotContain(SECRET, cut.Markup);
    }

    [Fact]
    public void ChangingTab_ClearsTheKey_AndDoesNotBringItBack()
    {
        var cut = RenderPanel();
        Reveal(cut, "Copy key");

        ShowTab(cut, 1);
        ShowTab(cut, 0);

        Assert.DoesNotContain(SECRET, cut.Markup);
    }

    [Fact]
    public void Key_ClearsItself_AfterAShortTime()
    {
        var cut = RenderPanel();
        Reveal(cut, "Copy key");
        Assert.Contains(SECRET, cut.Markup);

        _time.FireTimers();

        cut.WaitForAssertion(() => Assert.DoesNotContain(SECRET, cut.Markup));
    }
}

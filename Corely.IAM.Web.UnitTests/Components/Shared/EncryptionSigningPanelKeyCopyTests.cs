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
    private readonly List<(
        KeyType KeyType,
        string? Password,
        string? TotpCode,
        string? GoogleIdToken,
        string? GoogleNonce
    )> _calls = [];
    private readonly List<KeyType> _plainRotations = [];
    private readonly List<KeyType> _compromisedRotations = [];
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

    private IRenderedComponent<EncryptionSigningPanel> RenderPanel(
        bool reveal = true,
        string? googleClientId = null,
        bool canProveIdentity = true,
        IIamAsymmetricSignatureProvider? signing = null
    ) =>
        Render<EncryptionSigningPanel>(p =>
        {
            p.Add(
                x => x.RotateKeyAsync,
                keyType =>
                {
                    _plainRotations.Add(keyType);
                    return Task.FromResult(
                        new ModifyResult(ModifyResultCode.Success, string.Empty)
                    );
                }
            );
            p.Add(x => x.SigProvider, signing);
            p.Add(x => x.RevealGoogleClientId, googleClientId);
            p.Add(x => x.CanProveIdentity, canProveIdentity);
            if (reveal)
            {
                p.Add(
                    x => x.RotateCompromisedKeyAsync,
                    keyType =>
                    {
                        _compromisedRotations.Add(keyType);
                        return Task.FromResult(
                            new ModifyResult(ModifyResultCode.Success, string.Empty)
                        );
                    }
                );
                p.Add(
                    x => x.RevealKeyAsync,
                    (keyType, password, totpCode, googleIdToken, googleNonce) =>
                    {
                        _calls.Add((keyType, password, totpCode, googleIdToken, googleNonce));
                        return Task.FromResult(_result);
                    }
                );
            }
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

        Assert.Equal((keyType, "pw", "123456", null, null), Assert.Single(_calls));
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

    [Fact]
    public void GoogleOnlyUser_IsAskedForAGoogleSignIn_InsteadOfAPassword()
    {
        var module = JSInterop.SetupModule("./_content/Corely.IAM.Web/js/google-link.js");
        module.SetupVoid("renderReauthButton", _ => true).SetVoidResult();
        var cut = RenderPanel(googleClientId: "client-id");

        Button(cut, "Copy key").Click();

        Assert.Empty(cut.FindAll(".modal-body input[type=password]"));
        Assert.Contains("Sign in with Google again", cut.Find(".modal-body").TextContent);
        cut.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["renderReauthButton"]));
        var arguments = module.Invocations["renderReauthButton"][0].Arguments;
        Assert.Equal("client-id", arguments[1]);
        Assert.False(string.IsNullOrWhiteSpace((string?)arguments[2]));
    }

    [Fact]
    public void GoogleProof_IsSentWithTheNonceItWasIssuedFor_AndOnlyOnce()
    {
        var module = JSInterop.SetupModule("./_content/Corely.IAM.Web/js/google-link.js");
        module.SetupVoid("renderReauthButton", _ => true).SetVoidResult();
        var cut = RenderPanel(googleClientId: "client-id");

        Button(cut, "Copy key").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["renderReauthButton"]));
        var nonce = (string?)module.Invocations["renderReauthButton"][0].Arguments[2];
        cut.InvokeAsync(() => cut.Instance.OnGoogleReauthCredential("id-token"));
        Button(cut, "Show key").Click();

        var call = Assert.Single(_calls);
        Assert.Empty(call.Password ?? string.Empty);
        Assert.Equal("id-token", call.GoogleIdToken);
        Assert.Equal(nonce, call.GoogleNonce);

        cut.Find("button[title='Hide']").Click();
        Button(cut, "Copy key").Click();
        Button(cut, "Show key").Click();

        Assert.Null(_calls[1].GoogleIdToken);
        Assert.NotEqual(nonce, _calls[1].GoogleNonce);
    }

    [Fact]
    public void UserWithNoWayToProveThemselves_IsToldWhatToDo_InsteadOfADeadEnd()
    {
        var cut = RenderPanel(canProveIdentity: false);

        Button(cut, "Copy key").Click();

        Assert.Empty(cut.FindAll(".modal-body"));
        Assert.Contains(
            "Set a password on your profile",
            cut.Find(".tab-content .alert-danger").TextContent
        );
        Assert.Empty(_calls);
    }

    [Fact]
    public void Rotate_OffersTheCompromisedOption_AndAPlainRotationStaysPlain()
    {
        var cut = RenderPanel();

        Button(cut, "Rotate key").Click();
        Assert.DoesNotContain("cannot be undone", cut.Find(".modal-body").TextContent);
        cut.Find(".modal-footer .btn-warning").Click();

        Assert.Equal([KeyType.SymmetricEncryption], _plainRotations);
        Assert.Empty(_compromisedRotations);
    }

    [Theory]
    [InlineData(0, KeyType.SymmetricEncryption)]
    [InlineData(1, KeyType.AsymmetricEncryption)]
    [InlineData(2, KeyType.AsymmetricSignature)]
    public void Rotate_MarksTheOldKeyCompromised_AfterWarningItCannotBeUndone(
        int tab,
        KeyType keyType
    )
    {
        var cut = RenderPanel();
        ShowTab(cut, tab);

        Button(cut, "Rotate key").Click();
        cut.Find("#mark-key-compromised").Change(true);

        Assert.Contains("cannot be undone", cut.Find(".modal-body .alert-danger").TextContent);
        cut.Find(".modal-footer .btn-warning").Click();

        Assert.Equal([keyType], _compromisedRotations);
        Assert.Empty(_plainRotations);
        Assert.Contains("old key marked compromised", cut.Markup);
    }

    [Fact]
    public void Panel_WithoutTheOption_ShowsThePlainRotateConfirmation()
    {
        var cut = RenderPanel(reveal: false);

        Button(cut, "Rotate key").Click();

        Assert.Empty(cut.FindAll("#mark-key-compromised"));
        Assert.Contains("Rotate this encryption key?", cut.Find(".modal-body").TextContent);
    }

    [Fact]
    public void Panel_ListsCompromisedVersions_WithWhatTheyStillDo()
    {
        var signing = new Mock<IIamAsymmetricSignatureProvider>();
        signing.Setup(x => x.CompromisedVersions).Returns([1, 2]);
        signing.Setup(x => x.Version).Returns(3);
        var cut = RenderPanel(signing: signing.Object);

        ShowTab(cut, 2);

        var notice = cut.Find(".tab-content .alert-warning").TextContent;
        Assert.Contains("Compromised: version 1, 2.", notice);
        Assert.Contains("do not verify", notice);
    }
}

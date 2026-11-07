using AngleSharp.Dom;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Extensions;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class EncryptionSigningPanelKeyCopyTests : BunitContext
{
    private const string SECRET = "dGhlLXNlY3JldC1rZXk=";
    private const string CURRENT_PUBLIC_KEY = "current-public-key";
    private const string OLDER_PUBLIC_KEY = "older-public-key";

    private readonly ManualTimeProvider _time = new();
    private readonly List<(KeyType KeyType, string? Password, string? TotpCode)> _calls = [];
    private readonly List<KeyType> _plainRotations = [];
    private readonly List<(KeyType KeyType, string? Password)> _compromisedRotations = [];
    private readonly List<(KeyType KeyType, int Version, string? Password)> _markedVersions = [];
    private CompromiseAccountKeyResult _compromiseResult = new(
        CompromiseAccountKeyResultCode.Success,
        string.Empty
    );
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
        IReadOnlyList<IdentityProviderOption>? proofProviders = null,
        bool canProveIdentity = true,
        IIamAsymmetricSignatureProvider? signing = null,
        IIamSymmetricEncryptionProvider? symmetric = null,
        bool showVersions = false
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
            p.Add(x => x.SymProvider, symmetric);
            p.Add(x => x.ShowKeyVersions, showVersions);
            p.Add(x => x.ProofProviders, proofProviders);
            p.Add(x => x.CanProveIdentity, canProveIdentity);
            if (reveal)
            {
                p.Add(
                    x => x.RotateCompromisedKeyAsync,
                    (keyType, password, _) =>
                    {
                        _compromisedRotations.Add((keyType, password));
                        return Task.FromResult(_compromiseResult);
                    }
                );
                p.Add(
                    x => x.MarkKeyCompromisedAsync,
                    (keyType, version, password, _) =>
                    {
                        _markedVersions.Add((keyType, version, password));
                        return Task.FromResult(_compromiseResult);
                    }
                );
                p.Add(
                    x => x.RevealKeyAsync,
                    (keyType, password, totpCode) =>
                    {
                        _calls.Add((keyType, password, totpCode));
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

    private static readonly IReadOnlyList<IdentityProviderOption> _googleOnly =
    [
        new(IdentityProviderType.Google, "Google", true),
    ];

    [Fact]
    public void FederatedOnlyUser_IsOfferedTheirLinkedProviders_InsteadOfAPassword()
    {
        var cut = RenderPanel(proofProviders: _googleOnly);

        Button(cut, "Copy key").Click();

        Assert.Empty(cut.FindAll(".modal-body input[type=password]"));
        Assert.Contains("Sign in with Google", cut.Find(".modal-body").TextContent);
    }

    [Fact]
    public void ContinueWith_StartsAReauthentication_ThatReturnsToTheSameAction()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounts/keys");
        var cut = RenderPanel(proofProviders: _googleOnly);

        Button(cut, "Copy key").Click();
        Button(cut, "Sign in with Google").Click();

        var uri = Uri.UnescapeDataString(navigation.Uri);
        Assert.Contains("/signin/start/google?purpose=reauthenticate", uri);
        Assert.Contains(
            $"returnUrl=/accounts/keys?proof=reveal&key={KeyType.SymmetricEncryption}&proved=1",
            uri
        );
    }

    [Fact]
    public void ReturningFromAProof_ReopensTheAction_AndSendsNoPassword()
    {
        Services
            .GetRequiredService<NavigationManager>()
            .NavigateTo($"/accounts/keys?proof=reveal&key={KeyType.SymmetricEncryption}&proved=1");
        var cut = RenderPanel(proofProviders: _googleOnly);

        cut.WaitForAssertion(() =>
            Assert.Contains("You signed in again", cut.Find(".modal-body").TextContent)
        );
        Button(cut, "Show key").Click();

        var call = Assert.Single(_calls);
        Assert.Equal(KeyType.SymmetricEncryption, call.KeyType);
        Assert.True(string.IsNullOrEmpty(call.Password));
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
    public void Rotate_MarksTheOldKeyCompromised_OnlyAfterWarningAndProof(int tab, KeyType keyType)
    {
        var cut = RenderPanel();
        ShowTab(cut, tab);

        Button(cut, "Rotate key").Click();
        cut.Find("#mark-key-compromised").Change(true);
        Assert.Contains("cannot be undone", cut.Find(".modal-body .alert-danger").TextContent);
        cut.Find(".modal-footer .btn-warning").Click();

        Assert.Empty(_compromisedRotations);
        Assert.Empty(_plainRotations);
        Assert.Contains("cannot be undone", cut.Find(".modal-body .alert-danger").TextContent);
        cut.Find(".modal-body input[type=password]").Change("pw");
        Button(cut, "Rotate and mark compromised").Click();

        Assert.Equal([(keyType, "pw")], _compromisedRotations);
        Assert.Empty(_plainRotations);
        Assert.Contains("old key marked compromised", cut.Markup);
    }

    [Fact]
    public void Rotate_ShowsTheRefusal_ForAWrongPassword()
    {
        _compromiseResult = new CompromiseAccountKeyResult(
            CompromiseAccountKeyResultCode.InvalidCurrentPasswordError,
            "Current password is incorrect"
        );
        var cut = RenderPanel();

        Button(cut, "Rotate key").Click();
        cut.Find("#mark-key-compromised").Change(true);
        cut.Find(".modal-footer .btn-warning").Click();
        cut.Find(".modal-body input[type=password]").Change("wrong");
        Button(cut, "Rotate and mark compromised").Click();

        Assert.Contains("Current password is incorrect", cut.Find(".alert-danger").TextContent);
        Assert.DoesNotContain("old key marked compromised", cut.Markup);
    }

    [Fact]
    public void Rotate_ExplainsWhatToDo_ForAUserWithNoWayToProveThemselves()
    {
        var cut = RenderPanel(canProveIdentity: false);

        Button(cut, "Rotate key").Click();
        cut.Find("#mark-key-compromised").Change(true);
        cut.Find(".modal-footer .btn-warning").Click();

        Assert.Empty(cut.FindAll(".modal-body"));
        Assert.Contains("Set a password on your profile", cut.Find(".alert-danger").TextContent);
        Assert.Empty(_compromisedRotations);
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

    [Fact]
    public void PublicKeysAddress_ShowsAboveTheTabs_WithAPlainLineAndACopyButton()
    {
        const string address = "https://host/keys/1/.well-known/jwks.json";

        var cut = Render<EncryptionSigningPanel>(p => p.Add(x => x.PublicKeysAddress, address));

        var field = cut.Find(".section-divider > div.mb-3");
        Assert.Contains(address, field.TextContent);
        Assert.Contains("nothing here is secret", field.TextContent);
        field.QuerySelector("button")!.Click();
        Assert.Contains(
            JSInterop.Invocations,
            i => i.Identifier == "navigator.clipboard.writeText" && Equals(i.Arguments[0], address)
        );
    }

    [Fact]
    public void PublicKeysAddress_IsHidden_WhenTheHostHasNotMappedIt()
    {
        var cut = Render<EncryptionSigningPanel>();

        Assert.DoesNotContain("Public keys address", cut.Markup);
    }

    private static Mock<IIamAsymmetricSignatureProvider> SigningWithVersions(
        params KeyVersionInfo[] versions
    )
    {
        var signing = new Mock<IIamAsymmetricSignatureProvider>();
        signing.Setup(x => x.ProviderName).Returns("RSA-3072");
        signing.Setup(x => x.Version).Returns(versions.Max(v => v.Version));
        signing.Setup(x => x.PublicKey).Returns(CURRENT_PUBLIC_KEY);
        signing.Setup(x => x.Versions).Returns(versions);
        return signing;
    }

    private static readonly KeyVersionInfo[] _signingVersions =
    [
        new(1, "ED25519", OLDER_PUBLIC_KEY, true),
        new(2, "RSA-2048", OLDER_PUBLIC_KEY, false),
        new(3, "RSA-3072", CURRENT_PUBLIC_KEY, false),
    ];

    private static void SelectVersion(
        IRenderedComponent<EncryptionSigningPanel> cut,
        int version
    ) => cut.Find("select[aria-label='Key version']").Change(version.ToString());

    private static IReadOnlyList<string> ButtonTexts(
        IRenderedComponent<EncryptionSigningPanel> cut
    ) => [.. cut.FindAll(".tab-content button").Select(b => b.TextContent.Trim())];

    [Fact]
    public void VersionDropdown_ListsEveryVersionNewestFirst_AndDefaultsToTheCurrentOne()
    {
        var cut = RenderPanel(
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );

        ShowTab(cut, 2);

        var options = cut.FindAll("select[aria-label='Key version'] option");
        Assert.Equal(
            ["Version 3 (current)", "Version 2", "Version 1 (compromised)"],
            options.Select(o => o.TextContent.Trim())
        );
        Assert.Equal(
            ["3"],
            options.Where(o => o.HasAttribute("selected")).Select(o => o.GetAttribute("value"))
        );
    }

    [Fact]
    public void VersionDropdown_IsAbsent_WithoutTheOption()
    {
        var cut = RenderPanel(signing: SigningWithVersions(_signingVersions).Object);

        ShowTab(cut, 2);

        Assert.Empty(cut.FindAll("select"));
        Assert.Contains("Version 3", cut.Markup);
    }

    [Fact]
    public void CurrentVersion_KeepsItsToolsAndRotate_WithTheDropdown()
    {
        var cut = RenderPanel(
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );

        ShowTab(cut, 2);

        var buttons = ButtonTexts(cut);
        Assert.Contains("Sign", buttons);
        Assert.Contains("Verify", buttons);
        Assert.Contains("Copy private key", buttons);
        Assert.Contains("Rotate key", buttons);
        Assert.DoesNotContain("Mark as compromised", buttons);
        Assert.Contains(CURRENT_PUBLIC_KEY, cut.Find(".tab-content textarea").TextContent);
    }

    [Fact]
    public void OlderVersion_ShowsThatVersion_AndOffersOnlyMarkAsCompromised()
    {
        var cut = RenderPanel(
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );
        ShowTab(cut, 2);

        SelectVersion(cut, 2);

        Assert.Equal("RSA-2048", cut.Find(".tab-content .badge").TextContent);
        Assert.Contains(OLDER_PUBLIC_KEY, cut.Find(".tab-content textarea").TextContent);
        Assert.DoesNotContain(CURRENT_PUBLIC_KEY, cut.Markup);
        Assert.Contains("New signatures only use the current version", cut.Markup);
        Assert.Equal(["Mark as compromised"], ButtonTexts(cut).Where(t => t.Length > 0));
    }

    [Fact]
    public void OlderSymmetricVersion_HasNoPublicKey_AndNoEncryptionTools()
    {
        var symmetric = new Mock<IIamSymmetricEncryptionProvider>();
        symmetric.Setup(x => x.ProviderName).Returns("AES-256-GCM");
        symmetric.Setup(x => x.Version).Returns(2);
        symmetric
            .Setup(x => x.Versions)
            .Returns([new(1, "AES-128-GCM", null, false), new(2, "AES-256-GCM", null, false)]);
        var cut = RenderPanel(symmetric: symmetric.Object, showVersions: true);

        SelectVersion(cut, 1);

        Assert.Equal("AES-128-GCM", cut.Find(".tab-content .badge").TextContent);
        Assert.Empty(cut.FindAll(".tab-content textarea"));
        Assert.Equal(["Mark as compromised"], ButtonTexts(cut));
    }

    [Fact]
    public void MarkAsCompromised_MarksThatVersion_OnlyAfterWarningAndProof_AndStaysOnIt()
    {
        var signing = SigningWithVersions(_signingVersions);
        var cut = RenderPanel(signing: signing.Object, showVersions: true);
        ShowTab(cut, 2);
        SelectVersion(cut, 2);

        Button(cut, "Mark as compromised").Click();

        Assert.Empty(_markedVersions);
        Assert.Contains("cannot be undone", cut.Find(".modal-body .alert-danger").TextContent);
        cut.Find(".modal-body input[type=password]").Change("pw");
        signing
            .Setup(x => x.Versions)
            .Returns([
                _signingVersions[0],
                _signingVersions[1] with
                {
                    IsCompromised = true,
                },
                _signingVersions[2],
            ]);
        Button(cut, "Mark compromised").Click();

        Assert.Equal([(KeyType.AsymmetricSignature, 2, "pw")], _markedVersions);
        Assert.Contains("Version 2 marked compromised.", cut.Markup);
        Assert.Contains(
            "Version 2 is compromised.",
            cut.Find(".tab-content .alert-danger").TextContent
        );
        Assert.DoesNotContain("Mark as compromised", ButtonTexts(cut));
        Assert.Contains(
            "Version 2 (compromised)",
            cut.FindAll("option")
                .Where(o => o.HasAttribute("selected"))
                .Select(o => o.TextContent.Trim())
        );
    }

    [Fact]
    public void CompromisedVersion_SaysSo_AndOffersNoAction()
    {
        var cut = RenderPanel(
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );
        ShowTab(cut, 2);

        SelectVersion(cut, 1);

        Assert.Contains(
            "Version 1 is compromised.",
            cut.Find(".tab-content .alert-danger").TextContent
        );
        Assert.Empty(ButtonTexts(cut).Where(t => t.Length > 0));
    }

    [Fact]
    public void OlderVersion_OffersNoAction_WithoutTheCallback()
    {
        var cut = RenderPanel(
            reveal: false,
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );
        ShowTab(cut, 2);

        SelectVersion(cut, 2);

        Assert.DoesNotContain("Mark as compromised", ButtonTexts(cut));
    }

    [Fact]
    public void SelectingTheCurrentVersionAgain_FollowsTheKey_WhenItLaterRotates()
    {
        var signing = SigningWithVersions(_signingVersions);
        var cut = RenderPanel(signing: signing.Object, showVersions: true);
        ShowTab(cut, 2);
        SelectVersion(cut, 2);
        SelectVersion(cut, 3);

        signing.Setup(x => x.Version).Returns(4);
        signing
            .Setup(x => x.Versions)
            .Returns([.. _signingVersions, new(4, "RSA-3072", CURRENT_PUBLIC_KEY, false)]);
        cut.Render();

        Assert.Contains("Rotate key", ButtonTexts(cut));
    }

    [Fact]
    public void SelectingAVersion_ClearsARevealedKey()
    {
        var cut = RenderPanel(
            signing: SigningWithVersions(_signingVersions).Object,
            showVersions: true
        );
        ShowTab(cut, 2);
        Reveal(cut, "Copy private key");
        Assert.Contains(SECRET, cut.Markup);

        SelectVersion(cut, 2);
        Assert.DoesNotContain(SECRET, cut.Markup);

        SelectVersion(cut, 3);
        Assert.DoesNotContain(SECRET, cut.Markup);
    }
}

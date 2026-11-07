using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Components.Platform;
using Corely.IAM.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Platform;

public class IdentityProvidersSectionTests : TestContext
{
    private readonly Mock<IPlatformService> _platform = new();
    private UpdateIdentityProviderRequest? _saved;

    public IdentityProvidersSectionTests()
    {
        Services.AddSingleton(_platform.Object);
        Services.AddSingleton(new Mock<IBlazorUserContextAccessor>().Object);
        _platform
            .Setup(p => p.GetIdentityProvidersAsync())
            .ReturnsAsync(
                new GetIdentityProvidersResult(
                    RetrieveResultCode.Success,
                    "",
                    [
                        new(
                            IdentityProviderType.Google,
                            true,
                            true,
                            "google-client",
                            IdentityProviderCredentialKind.ClientSecret,
                            true,
                            null,
                            null
                        ),
                        new(
                            IdentityProviderType.Microsoft,
                            false,
                            true,
                            "",
                            IdentityProviderCredentialKind.Certificate,
                            false,
                            null,
                            null
                        ),
                    ]
                )
            );
        _platform
            .Setup(p => p.UpdateIdentityProviderAsync(It.IsAny<UpdateIdentityProviderRequest>()))
            .Callback<UpdateIdentityProviderRequest>(r => _saved = r)
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));
    }

    private IRenderedComponent<IdentityProvidersSection> RenderSection(bool canUpdate = true) =>
        Render<IdentityProvidersSection>(p => p.Add(x => x.CanUpdate, canUpdate));

    private static void ClickIn(
        IRenderedComponent<IdentityProvidersSection> cut,
        string provider,
        string text
    ) =>
        cut.Find($"[data-identity-provider={provider}]")
            .QuerySelectorAll("button")
            .First(b => b.TextContent.Trim() == text)
            .Click();

    [Fact]
    public void Section_ShowsEachProvider_AndNeverASecret()
    {
        var cut = RenderSection();

        Assert.Contains("google-client", cut.Find("[data-identity-provider=Google]").TextContent);
        Assert.Contains("Set", cut.Find("[data-identity-provider=Google]").TextContent);
        Assert.Contains(
            "Not configured",
            cut.Find("[data-identity-provider=Microsoft]").TextContent
        );
    }

    [Fact]
    public void Section_OffersNoEdit_WithoutUpdate()
    {
        var cut = RenderSection(canUpdate: false);

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Edit");
    }

    [Fact]
    public void Microsoft_OffersACertificate_AsThePreferredCredential()
    {
        var cut = RenderSection();

        ClickIn(cut, "Microsoft", "Edit");

        Assert.Contains("Certificate (preferred)", cut.Markup);
        Assert.Contains("Client secret", cut.Markup);
    }

    [Fact]
    public void Save_KeepsTheSecret_WhenNoNewOneIsEntered()
    {
        var cut = RenderSection();

        ClickIn(cut, "Google", "Edit");
        cut.Find("#idp-signup").Change(false);
        ClickIn(cut, "Google", "Save");

        Assert.NotNull(_saved);
        Assert.False(_saved.AllowSignUp);
        Assert.True(_saved.Enabled);
        Assert.Equal("google-client", _saved.ClientId);
    }

    [Fact]
    public void TurningOff_WarnsWithTheCount_AndSavesOnlyOnceConfirmed()
    {
        _platform
            .Setup(p => p.CountSoleSignInUsersAsync(IdentityProviderType.Google))
            .ReturnsAsync(new CountSoleSignInUsersResult(RetrieveResultCode.Success, "", 37));
        var cut = RenderSection();

        ClickIn(cut, "Google", "Edit");
        cut.Find("#idp-enabled").Change(false);
        ClickIn(cut, "Google", "Save");

        Assert.Null(_saved);
        Assert.Contains("37 users sign in only with Google", cut.Find(".modal-body").TextContent);

        cut.Find(".modal-footer .btn-danger").Click();

        Assert.NotNull(_saved);
        Assert.False(_saved.Enabled);
    }
}

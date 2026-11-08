using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Services;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Shared;

public class LinkedIdentitiesSectionTests : TestContext
{
    private readonly Mock<IFederatedIdentityService> _identities = new();
    private readonly Mock<IFederatedSignInService> _signIn = new();
    private IReadOnlyList<IdentityProviderOption> _providers =
    [
        new(IdentityProviderType.Google, "Google", true),
        new(IdentityProviderType.GitHub, "GitHub", true),
    ];

    public LinkedIdentitiesSectionTests()
    {
        Services.AddSingleton<IOperationScopes>(new InlineOperationScopes(Services));
        Services.AddSingleton(_identities.Object);
        Services.AddSingleton(_signIn.Object);
        Services.AddSingleton(new Mock<IBlazorUserContextAccessor>().Object);
        _signIn.Setup(s => s.ListIdentityProvidersAsync()).ReturnsAsync(() => _providers);
    }

    private IRenderedComponent<LinkedIdentitiesSection> RenderSection(
        params LinkedIdentity[] linked
    ) => Render<LinkedIdentitiesSection>(p => p.Add(x => x.LinkedIdentities, linked));

    private static void Click(
        IRenderedComponent<LinkedIdentitiesSection> cut,
        string provider,
        string text
    ) =>
        cut.Find($"[data-identity-provider={provider}]")
            .QuerySelectorAll("button")
            .First(b => b.TextContent.Trim() == text)
            .Click();

    [Fact]
    public void Section_ListsOnlyTheEnabledProviders_WithWhatIsLinked()
    {
        var cut = RenderSection(
            new LinkedIdentity(IdentityProviderType.Google, "me@gmail.com", DateTime.UtcNow)
        );

        Assert.Contains("me@gmail.com", cut.Find("[data-identity-provider=Google]").TextContent);
        Assert.Contains("Not linked", cut.Find("[data-identity-provider=GitHub]").TextContent);
        Assert.Empty(cut.FindAll("[data-identity-provider=Facebook]"));
    }

    [Fact]
    public void Section_IsHidden_WhenNoProviderIsOn()
    {
        _providers = [];

        var cut = RenderSection(
            new LinkedIdentity(IdentityProviderType.Google, "me@gmail.com", DateTime.UtcNow)
        );

        Assert.DoesNotContain("Linked Accounts", cut.Markup);
    }

    [Fact]
    public void Link_GoesToTheProvider_AndReturnsHere()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/profile");
        var cut = RenderSection();

        Click(cut, "GitHub", "Link");

        Assert.Equal(
            "http://localhost/signin/start/github?purpose=link&returnUrl=%2Fprofile",
            navigation.Uri
        );
    }

    [Fact]
    public void Unlink_AsksFirst_ThenUnlinks()
    {
        _identities
            .Setup(s => s.UnlinkFederatedIdentityAsync(IdentityProviderType.Google))
            .ReturnsAsync(
                new UnlinkFederatedIdentityResult(UnlinkFederatedIdentityResultCode.Success, "")
            );
        var cut = RenderSection(
            new LinkedIdentity(IdentityProviderType.Google, "me@gmail.com", DateTime.UtcNow)
        );

        Click(cut, "Google", "Unlink");
        _identities.Verify(
            s => s.UnlinkFederatedIdentityAsync(It.IsAny<IdentityProviderType>()),
            Times.Never
        );
        cut.Find(".modal-footer button:last-child").Click();

        _identities.Verify(
            s => s.UnlinkFederatedIdentityAsync(IdentityProviderType.Google),
            Times.Once
        );
        Assert.Contains("Google account unlinked", cut.Markup);
    }

    [Fact]
    public void Unlink_ShowsTheRefusal()
    {
        _identities
            .Setup(s => s.UnlinkFederatedIdentityAsync(IdentityProviderType.Google))
            .ReturnsAsync(
                new UnlinkFederatedIdentityResult(
                    UnlinkFederatedIdentityResultCode.LastAuthMethodError,
                    "Cannot unlink the only way to sign in"
                )
            );
        var cut = RenderSection(
            new LinkedIdentity(IdentityProviderType.Google, "me@gmail.com", DateTime.UtcNow)
        );

        Click(cut, "Google", "Unlink");
        cut.Find(".modal-footer button:last-child").Click();

        Assert.Contains("Cannot unlink the only way to sign in", cut.Markup);
    }
}

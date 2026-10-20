using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Web.Components.Platform;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Platform;

public class PlatformSettingsRedirectTests : TestContext
{
    private const string SECURITY_PATH = "/settings/security";

    private readonly Mock<IAuthorizationProvider> _authorizationProvider = new();

    public PlatformSettingsRedirectTests()
    {
        var userContextAccessor = new Mock<IBlazorUserContextAccessor>();
        userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());
        Services.AddSingleton(userContextAccessor.Object);
        Services.AddSingleton(_authorizationProvider.Object);
        Services.Configure<IAMWebOptions>(o =>
        {
            o.PlatformSettingsPages.Add(
                new("Audit", "/settings/audit", "bi-journal-text", PlatformSettings.RESOURCE_TYPE)
            );
            o.PlatformSettingsPages.Add(
                new("Security", SECURITY_PATH, "bi-shield-lock", SecurityPolicy.RESOURCE_TYPE)
            );
        });
    }

    private void CanRead(params string[] resourceTypes) =>
        _authorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync((AuthAction _, string type, Guid[] _) => resourceTypes.Contains(type));

    [Fact]
    public void Redirect_OpensTheFirstPageTheUserCanRead()
    {
        CanRead(SecurityPolicy.RESOURCE_TYPE);
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<PlatformSettingsRedirect>();

        Assert.EndsWith(SECURITY_PATH, navigation.Uri);
    }

    [Fact]
    public void Redirect_SaysSo_WhenNoPageIsReadable()
    {
        CanRead();

        var cut = Render<PlatformSettingsRedirect>();

        cut.WaitForAssertion(() =>
            Assert.Contains("You do not have access to any platform settings.", cut.Markup)
        );
    }
}

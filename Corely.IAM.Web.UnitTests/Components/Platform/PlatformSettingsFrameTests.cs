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

public class PlatformSettingsFrameTests : TestContext
{
    private const string HOST_TYPE = "host_settings";
    private const string HOST_PATH = "/platform-settings/host";
    private const string AUDIT_PATH = "/settings/audit";
    private const string SECURITY_PATH = "/settings/security";

    private readonly Mock<IAuthorizationProvider> _authorizationProvider = new();

    public PlatformSettingsFrameTests()
    {
        var userContextAccessor = new Mock<IBlazorUserContextAccessor>();
        userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());
        Services.AddSingleton<IIamWebLinks>(new FakeIamWebLinks());
        Services.AddSingleton(userContextAccessor.Object);
        Services.AddSingleton(_authorizationProvider.Object);
        Services.Configure<IAMWebOptions>(o =>
        {
            o.PlatformSettingsPages.Add(
                new("Audit", AUDIT_PATH, "bi-journal-text", PlatformSettings.RESOURCE_TYPE)
            );
            o.PlatformSettingsPages.Add(
                new("Security", SECURITY_PATH, "bi-shield-lock", SecurityPolicy.RESOURCE_TYPE)
            );
            o.PlatformSettingsPages.Add(new("Host", HOST_PATH, "bi-gear", HOST_TYPE));
        });
    }

    private void CanRead(params string[] resourceTypes) =>
        _authorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync((AuthAction _, string type, Guid[] _) => resourceTypes.Contains(type));

    private string[] MenuPaths(IRenderedComponent<PlatformSettingsFrame> cut) =>
        [.. cut.FindAll("nav a").Select(a => a.GetAttribute("href")!)];

    private IRenderedComponent<PlatformSettingsFrame> RenderFrame() =>
        Render<PlatformSettingsFrame>(p => p.AddChildContent("content"));

    [Fact]
    public void Menu_ListsTheHostsPagesInOrder_WhenAllAreReadable()
    {
        CanRead(PlatformSettings.RESOURCE_TYPE, SecurityPolicy.RESOURCE_TYPE, HOST_TYPE);

        var cut = RenderFrame();

        cut.WaitForAssertion(() =>
            Assert.Equal([AUDIT_PATH, SECURITY_PATH, HOST_PATH], MenuPaths(cut))
        );
    }

    [Fact]
    public void Menu_LeavesOutPagesWhoseTypeTheUserCannotRead()
    {
        CanRead(SecurityPolicy.RESOURCE_TYPE);

        var cut = RenderFrame();

        cut.WaitForAssertion(() => Assert.Equal([SECURITY_PATH], MenuPaths(cut)));
        Assert.Contains("content", cut.Markup);
    }
}

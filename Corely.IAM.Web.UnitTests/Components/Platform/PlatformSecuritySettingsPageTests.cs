using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Services;
using Corely.IAM.Web.Components;
using Corely.IAM.Web.Components.Pages.Platform;
using Corely.IAM.Web.Services;
using Corely.IAM.Web.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestContext = Bunit.TestContext;

namespace Corely.IAM.Web.UnitTests.Components.Platform;

public class PlatformSecuritySettingsPageTests : TestContext
{
    private readonly Mock<IPlatformService> _platformService = new();
    private readonly Mock<IAuthorizationProvider> _authorizationProvider = new();
    private readonly SecurityPolicy _settings = SecurityPolicy.Default with
    {
        MaxLoginAttempts = 7,
    };

    public PlatformSecuritySettingsPageTests()
    {
        var userContextAccessor = new Mock<IBlazorUserContextAccessor>();
        userContextAccessor
            .Setup(x => x.GetUserContextAsync())
            .ReturnsAsync(PageTestHelpers.CreateUserContext());
        Services.AddSingleton(userContextAccessor.Object);
        Services.AddSingleton(_platformService.Object);
        Services.AddSingleton(_authorizationProvider.Object);
        Services.AddSingleton<ILogger<EntityPageBase>>(NullLogger<EntityPageBase>.Instance);
        _platformService
            .Setup(x => x.GetSecuritySettingsAsync())
            .ReturnsAsync(new GetSecuritySettingsResult(RetrieveResultCode.Success, "", _settings));
        _authorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(
                    It.IsAny<Corely.IAM.Security.Constants.AuthAction>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid[]>()
                )
            )
            .ReturnsAsync(true);
    }

    [Fact]
    public void Page_ShowsTheSettingsInForce_AndTheRecommendedMinimum()
    {
        var cut = Render<PlatformSecuritySettingsPage>();

        cut.WaitForAssertion(() => Assert.Contains("8 or more is recommended", cut.Markup));
        Assert.Contains(">7<", cut.Markup.Replace(" ", ""));
    }

    [Fact]
    public void Save_SendsTheEditedSettings()
    {
        _platformService
            .Setup(x => x.UpdateSecuritySettingsAsync(It.IsAny<SecurityPolicy>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));
        var cut = Render<PlatformSecuritySettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").First(b => b.TextContent == "Edit"));

        cut.FindAll("button").First(b => b.TextContent == "Edit").Click();
        cut.FindAll("input[type=number]")[2].Change("4");
        cut.FindAll("button").First(b => b.TextContent == "Save").Click();

        var expected = _settings with { MaxLoginAttempts = 4 };
        _platformService.Verify(x => x.UpdateSecuritySettingsAsync(expected), Times.Once);
        cut.WaitForAssertion(() => Assert.Contains("Security settings saved.", cut.Markup));
    }

    [Fact]
    public void Save_ShowsTheRefusal()
    {
        _platformService
            .Setup(x => x.UpdateSecuritySettingsAsync(It.IsAny<SecurityPolicy>()))
            .ReturnsAsync(
                new ModifyResult(
                    ModifyResultCode.ValidationError,
                    "Max Login Attempts is out of range"
                )
            );
        var cut = Render<PlatformSecuritySettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").First(b => b.TextContent == "Edit"));

        cut.FindAll("button").First(b => b.TextContent == "Edit").Click();
        cut.FindAll("button").First(b => b.TextContent == "Save").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Max Login Attempts is out of range", cut.Markup)
        );
    }
}

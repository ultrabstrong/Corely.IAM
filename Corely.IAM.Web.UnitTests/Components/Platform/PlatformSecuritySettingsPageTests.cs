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
                    It.IsAny<Corely.IAM.Security.Enums.AuthAction>(),
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

        cut.FindAll("button").Where(b => b.TextContent == "Edit").ElementAt(1).Click();
        cut.FindAll("input[type=number]")[0].Change("4");
        cut.FindAll("button").First(b => b.TextContent == "Save").Click();
        cut.Find(".modal-footer .btn-primary").Click();

        var expected = _settings with { MaxLoginAttempts = 4 };
        _platformService.Verify(x => x.UpdateSecuritySettingsAsync(expected), Times.Once);
        cut.WaitForAssertion(() => Assert.Contains("Sign in settings saved.", cut.Markup));
    }

    [Fact]
    public void Save_ListsTheChangesAndSavesNothingUntilConfirmed()
    {
        var cut = Render<PlatformSecuritySettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").First(b => b.TextContent == "Edit"));

        cut.FindAll("button").Where(b => b.TextContent == "Edit").ElementAt(1).Click();
        cut.FindAll("input[type=number]")[0].Change("10");
        cut.FindAll("button").First(b => b.TextContent == "Save").Click();

        Assert.Contains("Failed sign ins before lockout:", cut.Find(".modal-body").TextContent);
        Assert.Contains("weakens security", cut.Find(".modal-body").TextContent);
        cut.Find(".modal-footer .btn-secondary").Click();
        _platformService.Verify(
            x => x.UpdateSecuritySettingsAsync(It.IsAny<SecurityPolicy>()),
            Times.Never
        );
        Assert.Equal("10", cut.FindAll("input[type=number]")[0].GetAttribute("value"));
    }

    [Fact]
    public void Edit_LeavesTheOtherSectionsEditable()
    {
        var cut = Render<PlatformSecuritySettingsPage>();
        cut.WaitForAssertion(() =>
            Assert.Equal(3, cut.FindAll("button").Count(b => b.TextContent == "Edit"))
        );

        cut.FindAll("button").Where(b => b.TextContent == "Edit").ElementAt(2).Click();

        Assert.Equal(2, cut.FindAll("button").Count(b => b.TextContent == "Edit"));
        Assert.Equal(2, cut.FindAll("input[type=number]").Count);
        Assert.Empty(cut.FindAll("input[type=checkbox]"));
    }

    [Fact]
    public void Save_SendsOnlyItsOwnSection_WhileAnotherIsOpen()
    {
        _platformService
            .Setup(x => x.UpdateSecuritySettingsAsync(It.IsAny<SecurityPolicy>()))
            .ReturnsAsync(new ModifyResult(ModifyResultCode.Success, ""));
        var cut = Render<PlatformSecuritySettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").First(b => b.TextContent == "Edit"));

        cut.FindAll("button").Where(b => b.TextContent == "Edit").ElementAt(1).Click();
        cut.FindAll("button").First(b => b.TextContent == "Edit").Click();
        cut.Find("input[aria-label='Minimum length']").Change("12");
        cut.Find("input[aria-label='Failed sign ins before lockout']").Change("4");
        cut.FindAll("button").Where(b => b.TextContent == "Save").ElementAt(1).Click();
        cut.Find(".modal-footer .btn-primary").Click();

        var expected = _settings with { MaxLoginAttempts = 4 };
        _platformService.Verify(x => x.UpdateSecuritySettingsAsync(expected), Times.Once);
        Assert.Equal("12", cut.Find("input[aria-label='Minimum length']").GetAttribute("value"));
    }

    [Fact]
    public void Page_ShowsRulesAsEnabledOrDisabled_AndRangesBesideValues()
    {
        var cut = Render<PlatformSecuritySettingsPage>();

        cut.WaitForAssertion(() =>
            Assert.Equal(
                _settings.RequireUppercase ? "Enabled" : "Disabled",
                cut.Find("#requireUppercase .badge").TextContent
            )
        );
        Assert.Contains(
            $"Range {SecurityPolicy.MAX_LOGIN_ATTEMPTS_MIN} to {SecurityPolicy.MAX_LOGIN_ATTEMPTS_MAX}",
            cut.Markup
        );
        Assert.Empty(cut.FindAll("input"));
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

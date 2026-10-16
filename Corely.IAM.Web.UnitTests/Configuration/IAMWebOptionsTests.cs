using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Web.Configuration;

namespace Corely.IAM.Web.UnitTests.Configuration;

public class IAMWebOptionsTests
{
    private const string HOST_TYPE = "host_settings";

    private readonly Mock<IAuthorizationProvider> _authorizationProvider = new();

    private void CanRead(params string[] resourceTypes) =>
        _authorizationProvider
            .Setup(x =>
                x.IsAuthorizedAsync(AuthAction.Read, It.IsAny<string>(), It.IsAny<Guid[]>())
            )
            .ReturnsAsync((AuthAction _, string type, Guid[] _) => resourceTypes.Contains(type));

    [Fact]
    public void PlatformSettingsPages_StartWithIamsAuditAndSecurity()
    {
        var pages = new IAMWebOptions().PlatformSettingsPages;

        Assert.Equal(
            [PlatformSettings.RESOURCE_TYPE, SecurityPolicy.RESOURCE_TYPE],
            pages.Select(p => p.ResourceType)
        );
        Assert.Equal(
            [AppRoutes.PlatformAuditSettings, AppRoutes.PlatformSecuritySettings],
            pages.Select(p => p.Path)
        );
    }

    [Fact]
    public async Task ReadablePlatformSettingsPages_KeepOnlyPagesWhoseTypeTheUserCanRead()
    {
        CanRead(SecurityPolicy.RESOURCE_TYPE, HOST_TYPE);
        var options = new IAMWebOptions();
        options.PlatformSettingsPages.Add(
            new("Host", "/platform-settings/host", "bi-gear", HOST_TYPE)
        );

        var readable = await options.ReadablePlatformSettingsPagesAsync(
            _authorizationProvider.Object
        );

        Assert.Equal(["Security", "Host"], readable.Select(p => p.Title));
    }

    [Fact]
    public async Task ReadablePlatformSettingsPages_IsEmpty_WhenNoTypeIsReadable()
    {
        CanRead();

        var readable = await new IAMWebOptions().ReadablePlatformSettingsPagesAsync(
            _authorizationProvider.Object
        );

        Assert.Empty(readable);
    }
}

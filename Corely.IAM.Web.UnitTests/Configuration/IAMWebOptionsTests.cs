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
    public void PlatformSettingsPages_StartEmpty_ForTheHostToFill()
    {
        Assert.Empty(new IAMWebOptions().PlatformSettingsPages);
    }

    [Fact]
    public void HomePath_DefaultsToRoot()
    {
        Assert.Equal("/", new IAMWebOptions().HomePath);
    }

    [Fact]
    public async Task ReadablePlatformSettingsPages_KeepOnlyPagesWhoseTypeTheUserCanRead()
    {
        CanRead(SecurityPolicy.RESOURCE_TYPE, HOST_TYPE);
        var options = new IAMWebOptions();
        options.PlatformSettingsPages.Add(
            new("Audit", "/settings/audit", "bi-journal-text", PlatformSettings.RESOURCE_TYPE)
        );
        options.PlatformSettingsPages.Add(
            new("Security", "/settings/security", "bi-shield-lock", SecurityPolicy.RESOURCE_TYPE)
        );
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

using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Web.Configuration;

public sealed class IAMWebOptions
{
    public string? ForgotPasswordPath { get; set; }

    public List<PlatformSettingsPage> PlatformSettingsPages { get; } =
    [
        new(
            "Audit",
            AppRoutes.PlatformAuditSettings,
            "bi-journal-text",
            PlatformSettings.RESOURCE_TYPE
        ),
        new(
            "Security",
            AppRoutes.PlatformSecuritySettings,
            "bi-shield-lock",
            SecurityPolicy.RESOURCE_TYPE
        ),
    ];

    internal async Task<IReadOnlyList<PlatformSettingsPage>> ReadablePlatformSettingsPagesAsync(
        IAuthorizationProvider authorizationProvider
    )
    {
        var readable = new List<PlatformSettingsPage>();
        foreach (var page in PlatformSettingsPages)
        {
            if (await authorizationProvider.IsAuthorizedAsync(AuthAction.Read, page.ResourceType))
                readable.Add(page);
        }
        return readable;
    }
}

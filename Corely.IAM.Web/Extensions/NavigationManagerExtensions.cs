using Microsoft.AspNetCore.Components;

namespace Corely.IAM.Web.Extensions;

internal static class NavigationManagerExtensions
{
    extension(NavigationManager navigationManager)
    {
        public string RelativeUri() =>
            "/" + navigationManager.ToBaseRelativePath(navigationManager.Uri);

        public string SignInUri() =>
            $"{AppRoutes.SignIn}?returnUrl={Uri.EscapeDataString(navigationManager.RelativeUri())}";

        public string SwitchAccountUri(Guid accountId) =>
            $"{AppRoutes.SwitchAccount}?accountId={accountId}&returnUrl={Uri.EscapeDataString(navigationManager.RelativeUri())}";
    }
}

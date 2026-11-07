using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.Web.Extensions;

internal static class IdentityProviderTypeExtensions
{
    extension(IdentityProviderType type)
    {
        public string IconClass() =>
            type switch
            {
                IdentityProviderType.Google => "bi-google",
                IdentityProviderType.Microsoft => "bi-microsoft",
                IdentityProviderType.GitHub => "bi-github",
                IdentityProviderType.Facebook => "bi-facebook",
                IdentityProviderType.Amazon => "bi-amazon",
                _ => "bi-box-arrow-in-right",
            };

        public string StartPath(FederatedSignInPurpose purpose, string? returnUrl)
        {
            var query = new List<string>();
            if (purpose != FederatedSignInPurpose.SignIn)
                query.Add($"purpose={purpose.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrWhiteSpace(returnUrl))
                query.Add($"returnUrl={Uri.EscapeDataString(returnUrl)}");
            var path = $"{AppRoutes.FederatedSignInStart}/{type.ToString().ToLowerInvariant()}";
            return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
        }
    }
}

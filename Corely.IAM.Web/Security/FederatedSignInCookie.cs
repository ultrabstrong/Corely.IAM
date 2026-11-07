using System.Security.Cryptography;
using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.IdentityProviders.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

internal sealed class FederatedSignInCookie(
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider
)
{
    public const string NAME = "__Host-federated_sign_in";
    public const string PROTECTION_PURPOSE = "Corely.IAM.Web.FederatedSignIn";

    private readonly IDataProtector _protector = dataProtectionProvider
        .ThrowIfNull(nameof(dataProtectionProvider))
        .CreateProtector(PROTECTION_PURPOSE);
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public void Set(HttpContext context, FederatedSignInState state) =>
        context.Response.Cookies.Append(
            NAME,
            _protector.Protect(JsonSerializer.Serialize(state)),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = _timeProvider
                    .GetUtcNow()
                    .AddSeconds(FederatedSignInCorrelation.MAX_AGE_SECONDS),
            }
        );

    public FederatedSignInState? Take(HttpContext context)
    {
        var value = context.Request.Cookies[NAME];
        if (value is null)
            return null;

        context.Response.Cookies.Delete(
            NAME,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            }
        );
        try
        {
            return JsonSerializer.Deserialize<FederatedSignInState>(_protector.Unprotect(value));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
    }
}

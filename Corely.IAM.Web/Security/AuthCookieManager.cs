using Corely.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

internal class AuthCookieManager(TimeProvider timeProvider) : IAuthCookieManager
{
    public const string AUTH_TOKEN_COOKIE = "authentication_token";
    public const string AUTH_TOKEN_ID_COOKIE = "auth_token_id";
    public const string DEVICE_ID_COOKIE = "device_id";
    public const string RETURN_URL_COOKIE = "return_url";

    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public void SetAuthCookies(
        IResponseCookies cookies,
        string authToken,
        Guid authTokenId,
        bool isHttps,
        int authCookieTtlSeconds
    )
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = _timeProvider.GetUtcNow().AddSeconds(authCookieTtlSeconds),
        };

        cookies.Append(AUTH_TOKEN_COOKIE, authToken, cookieOptions);
        cookies.Append(AUTH_TOKEN_ID_COOKIE, authTokenId.ToString(), cookieOptions);
    }

    public void DeleteAuthCookies(IResponseCookies cookies)
    {
        var cookieOptions = new CookieOptions { Path = "/" };
        cookies.Delete(AUTH_TOKEN_COOKIE, cookieOptions);
        cookies.Delete(AUTH_TOKEN_ID_COOKIE, cookieOptions);
    }

    public void DeleteDeviceIdCookie(IResponseCookies cookies)
    {
        cookies.Delete(DEVICE_ID_COOKIE, new CookieOptions { Path = "/" });
    }

    public string GetOrCreateDeviceId(HttpContext context)
    {
        var deviceId = context.Request.Cookies[DEVICE_ID_COOKIE];
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = Guid.CreateVersion7().ToString();
            context.Response.Cookies.Append(
                DEVICE_ID_COOKIE,
                deviceId,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = _timeProvider.GetUtcNow().AddDays(90),
                }
            );
        }
        return deviceId;
    }

    public void SetReturnUrlCookie(HttpContext context, string returnUrl)
    {
        context.Response.Cookies.Append(
            RETURN_URL_COOKIE,
            returnUrl,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = _timeProvider.GetUtcNow().AddMinutes(10),
            }
        );
    }

    public string? TakeReturnUrlCookie(HttpContext context)
    {
        var returnUrl = context.Request.Cookies[RETURN_URL_COOKIE];
        if (returnUrl != null)
        {
            context.Response.Cookies.Delete(RETURN_URL_COOKIE, new CookieOptions { Path = "/" });
        }
        return returnUrl;
    }
}

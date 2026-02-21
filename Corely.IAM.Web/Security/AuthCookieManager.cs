using Corely.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

public class AuthCookieManager(TimeProvider timeProvider) : IAuthCookieManager
{
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public void SetAuthCookies(
        IResponseCookies cookies,
        string authToken,
        Guid authTokenId,
        bool isHttps,
        int authTokenTtlSeconds
    )
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = _timeProvider.GetUtcNow().AddSeconds(authTokenTtlSeconds),
        };

        cookies.Append(AuthenticationConstants.AUTH_TOKEN_COOKIE, authToken, cookieOptions);
        cookies.Append(
            AuthenticationConstants.AUTH_TOKEN_ID_COOKIE,
            authTokenId.ToString(),
            cookieOptions
        );
    }

    public void DeleteAuthCookies(IResponseCookies cookies)
    {
        var cookieOptions = new CookieOptions { Path = "/" };
        cookies.Delete(AuthenticationConstants.AUTH_TOKEN_COOKIE, cookieOptions);
        cookies.Delete(AuthenticationConstants.AUTH_TOKEN_ID_COOKIE, cookieOptions);
    }

    public void DeleteDeviceIdCookie(IResponseCookies cookies)
    {
        cookies.Delete(AuthenticationConstants.DEVICE_ID_COOKIE, new CookieOptions { Path = "/" });
    }

    public string GetOrCreateDeviceId(HttpContext context)
    {
        var deviceId = context.Request.Cookies[AuthenticationConstants.DEVICE_ID_COOKIE];
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = Guid.CreateVersion7().ToString();
            context.Response.Cookies.Append(
                AuthenticationConstants.DEVICE_ID_COOKIE,
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
}

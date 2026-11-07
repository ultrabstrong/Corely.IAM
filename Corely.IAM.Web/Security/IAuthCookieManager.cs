using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

public interface IAuthCookieManager
{
    void SetAuthCookies(
        IResponseCookies cookies,
        string authToken,
        Guid authTokenId,
        string refreshToken,
        DateTime refreshExpiresUtc,
        bool isHttps
    );
    void DeleteAuthCookies(IResponseCookies cookies);
    void DeleteDeviceIdCookie(IResponseCookies cookies);
    string GetOrCreateDeviceId(HttpContext context);
}

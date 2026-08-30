using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

public interface IAuthCookieManager
{
    void SetAuthCookies(
        IResponseCookies cookies,
        string authToken,
        Guid authTokenId,
        bool isHttps,
        int authCookieTtlSeconds
    );
    void DeleteAuthCookies(IResponseCookies cookies);
    void DeleteDeviceIdCookie(IResponseCookies cookies);
    string GetOrCreateDeviceId(HttpContext context);
}

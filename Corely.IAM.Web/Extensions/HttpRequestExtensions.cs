using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

internal static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public bool IsSameOrigin() =>
            string.Equals(
                request.Headers["Sec-Fetch-Site"],
                "same-origin",
                StringComparison.OrdinalIgnoreCase
            );
    }
}

using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

internal static class HttpRequestExtensions
{
    private const string BEARER_SCHEME = "Bearer ";

    extension(HttpRequest request)
    {
        public bool IsSameOrigin() =>
            string.Equals(
                request.Headers["Sec-Fetch-Site"],
                "same-origin",
                StringComparison.OrdinalIgnoreCase
            );

        public bool HasBearerToken() =>
            request
                .Headers.Authorization.ToString()
                .StartsWith(BEARER_SCHEME, StringComparison.OrdinalIgnoreCase);

        public string? BearerToken()
        {
            var header = request.Headers.Authorization.ToString();
            if (!header.StartsWith(BEARER_SCHEME, StringComparison.OrdinalIgnoreCase))
                return null;
            var token = header[BEARER_SCHEME.Length..].Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
    }
}

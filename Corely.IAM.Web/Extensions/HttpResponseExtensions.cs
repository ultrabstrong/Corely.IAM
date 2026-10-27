using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Corely.IAM.Web.Extensions;

internal static class HttpResponseExtensions
{
    extension(HttpResponse response)
    {
        public Task BearerChallengeAsync(int statusCode)
        {
            response.StatusCode = statusCode;
            response.Headers[HeaderNames.WWWAuthenticate] =
                statusCode == StatusCodes.Status401Unauthorized
                && response.HttpContext.Items.ContainsKey(BearerTokenMiddleware.INVALID_TOKEN_ITEM)
                    ? "Bearer error=\"invalid_token\""
                    : "Bearer";
            return Task.CompletedTask;
        }
    }
}

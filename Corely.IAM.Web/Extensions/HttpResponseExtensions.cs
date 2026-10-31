using System.Net;
using Corely.IAM.Web.Endpoints;
using Corely.IAM.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Corely.IAM.Web.Extensions;

public static class HttpResponseExtensions
{
    extension(HttpResponse response)
    {
        internal Task BearerChallengeAsync(int statusCode)
        {
            response.StatusCode = statusCode;
            response.Headers[HeaderNames.WWWAuthenticate] =
                statusCode == StatusCodes.Status401Unauthorized
                && response.HttpContext.Items.ContainsKey(BearerTokenMiddleware.INVALID_TOKEN_ITEM)
                    ? "Bearer error=\"invalid_token\""
                    : "Bearer";
            return Task.CompletedTask;
        }

        public Task WriteApiFailureAsync(int statusCode, string? message = null)
        {
            response.StatusCode = statusCode;
            return response.WriteAsJsonAsync(
                ApiResponse.Failure(
                    ((HttpStatusCode)statusCode).ToString(),
                    message ?? ReasonPhrases.GetReasonPhrase(statusCode)
                )
            );
        }
    }
}

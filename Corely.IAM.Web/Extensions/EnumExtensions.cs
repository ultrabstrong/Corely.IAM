using Corely.IAM.Web.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

internal static class EnumExtensions
{
    extension(Enum resultCode)
    {
        public int ApiStatusCode() =>
            resultCode.ToString() switch
            {
                "Success" or "PartialSuccess" => StatusCodes.Status200OK,
                "UnauthorizedError" => StatusCodes.Status403Forbidden,
                "ValidationError"
                or "InvalidUserIdsError"
                or "InvalidRoleIdsError"
                or "InvalidPermissionIdsError"
                or "InvalidApiKeyIdsError" => StatusCodes.Status400BadRequest,
                "UserNotInAccountError" => StatusCodes.Status404NotFound,
                var name when name.EndsWith("NotFoundError", StringComparison.Ordinal) =>
                    StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict,
            };

        public IResult ToApiResult(string? message, object? data = null)
        {
            var status = resultCode.ApiStatusCode();
            return TypedResults.Json(
                new ApiResponse<object>(
                    resultCode.ToString(),
                    string.IsNullOrEmpty(message) ? null : message,
                    status == StatusCodes.Status200OK ? data : null
                ),
                statusCode: status
            );
        }
    }
}

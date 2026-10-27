using Corely.IAM.Web.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Extensions;

internal static class HttpContextExtensions
{
    extension(HttpContext context)
    {
        public bool IsIAMApiRequest() =>
            context.Request.HasBearerToken()
            || context.GetEndpoint()?.Metadata.GetMetadata<IamApiEndpointMetadata>() != null;
    }
}

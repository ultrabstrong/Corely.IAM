using Microsoft.AspNetCore.Routing;

namespace Corely.IAM.Web.Extensions;

internal static class LinkGeneratorExtensions
{
    extension(LinkGenerator links)
    {
        public string? AccountJwksPath(Guid accountId) =>
            links.GetPathByName(IamWebEndpointExtensions.JWKS_ENDPOINT_NAME, new { accountId });
    }
}

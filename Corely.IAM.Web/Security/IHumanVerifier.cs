using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

public interface IHumanVerifier
{
    Task<VerifyHumanResult> VerifyAsync(
        HttpContext httpContext,
        string action,
        CancellationToken cancellationToken
    );
}

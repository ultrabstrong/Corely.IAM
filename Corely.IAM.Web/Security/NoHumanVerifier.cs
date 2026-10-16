using Microsoft.AspNetCore.Http;

namespace Corely.IAM.Web.Security;

internal class NoHumanVerifier : IHumanVerifier
{
    private static readonly VerifyHumanResult _passed = new(
        VerifyHumanResultCode.Success,
        "No human check is configured"
    );

    public Task<VerifyHumanResult> VerifyAsync(
        HttpContext httpContext,
        string action,
        CancellationToken cancellationToken
    ) => Task.FromResult(_passed);
}

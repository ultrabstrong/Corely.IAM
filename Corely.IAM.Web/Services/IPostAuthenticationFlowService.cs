using Corely.IAM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SignInResultModel = Corely.IAM.Models.SignInResult;

namespace Corely.IAM.Web.Services;

public interface IPostAuthenticationFlowService
{
    Task<IActionResult> CompleteSignInAsync(
        HttpContext httpContext,
        SignInResultModel signInResult,
        int authTokenTtlSeconds
    );
}

using System.Security.Claims;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Web.Services;

public interface IUserContextClaimsBuilder
{
    ClaimsPrincipal BuildPrincipal(UserContext? userContext);
}

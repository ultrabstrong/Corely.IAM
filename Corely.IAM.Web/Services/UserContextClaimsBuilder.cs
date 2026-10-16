using System.Security.Claims;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Web.Services;

internal class UserContextClaimsBuilder : IUserContextClaimsBuilder
{
    public const string ACCOUNT_ID_CLAIM = "AccountId";
    public const string ACCOUNT_NAME_CLAIM = "AccountName";

    public ClaimsPrincipal BuildPrincipal(UserContext? userContext)
    {
        if (userContext?.User == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userContext.User.Id.ToString()),
            new(ClaimTypes.Name, userContext.User.Username),
        };

        if (!string.IsNullOrWhiteSpace(userContext.User.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, userContext.User.Email));
        }

        if (userContext.CurrentAccount != null)
        {
            claims.Add(new Claim(ACCOUNT_ID_CLAIM, userContext.CurrentAccount.Id.ToString()));
            claims.Add(new Claim(ACCOUNT_NAME_CLAIM, userContext.CurrentAccount.AccountName));
        }

        var identity = new ClaimsIdentity(claims, "Cookie");
        return new ClaimsPrincipal(identity);
    }
}

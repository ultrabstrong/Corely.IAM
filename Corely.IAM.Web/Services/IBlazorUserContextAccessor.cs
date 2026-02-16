using Corely.IAM.Users.Models;

namespace Corely.IAM.Web.Services;

public interface IBlazorUserContextAccessor
{
    Task<UserContext?> GetUserContextAsync();
}

using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

public interface IUserContextProvider
{
    UserContext? GetUserContext();
    void SetUserContext(UserContext context);
}

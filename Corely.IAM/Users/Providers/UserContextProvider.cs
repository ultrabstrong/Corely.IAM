using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

internal class UserContextProvider : IUserContextProvider
{
    private UserContext? _userContext;

    public UserContext? GetUserContext() => _userContext;

    public void SetUserContext(UserContext context) => _userContext = context;
}

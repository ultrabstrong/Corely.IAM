using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

internal class IamUserContextProvider : IIamUserContextProvider
{
    private IamUserContext? _userContext;

    public IamUserContext? GetUserContext() => _userContext;

    public void SetUserContext(IamUserContext context) => _userContext = context;
}

using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

public interface IIamUserContextProvider
{
    IamUserContext? GetUserContext();
    void SetUserContext(IamUserContext context);
}

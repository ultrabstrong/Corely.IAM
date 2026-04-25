using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

public interface IUserContextProvider
{
    UserContext? GetUserContext();
}

internal interface IUserContextSetter
{
    void SetUserContext(UserContext context);
    void SetSystemContext(string deviceId);
    void ClearUserContext(Guid userId);
}

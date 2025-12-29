using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

public interface IUserContextProvider
{
    UserContext? GetUserContext();
    Task<UserAuthTokenValidationResultCode> SetUserContextAsync(string authToken);
}

internal interface IUserContextSetter
{
    void SetUserContext(UserContext context);
    void ClearUserContext(Guid userId);
}

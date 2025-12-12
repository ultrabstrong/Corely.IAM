using Corely.IAM.Users.Models;

namespace Corely.IAM.Users.Providers;

public interface IIamUserContextProvider
{
    IamUserContext? GetUserContext();
    Task<UserAuthTokenValidationResultCode> SetUserContextAsync(string authToken);
}

internal interface IIamUserContextSetter
{
    void SetUserContext(IamUserContext context);
}

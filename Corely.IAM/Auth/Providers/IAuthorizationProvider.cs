using Corely.IAM.Auth.Constants;

namespace Corely.IAM.Auth.Providers;

public interface IAuthorizationProvider
{
    Task AuthorizeAsync(string resourceType, AuthAction action, int? resourceId = null);
}

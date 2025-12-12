using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Providers;

public interface IAuthorizationProvider
{
    Task AuthorizeAsync(string resourceType, AuthAction action, int? resourceId = null);
}

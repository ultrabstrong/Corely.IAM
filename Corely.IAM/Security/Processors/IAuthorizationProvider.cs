using Corely.IAM.Security.Constants;

namespace Corely.IAM.Security.Processors;

public interface IAuthorizationProvider
{
    Task AuthorizeAsync(string resourceType, AuthAction action, int? resourceId = null);
}

using Corely.IAM.Platform.Models;

namespace Corely.IAM.Platform.Providers;

public interface ISecurityPolicyProvider
{
    Task<SecurityPolicy> GetPolicyAsync();
}

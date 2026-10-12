using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;

namespace Corely.IAM.Web.UnitTests.Helpers;

internal class FixedSecurityPolicyProvider(SecurityPolicy? policy = null) : ISecurityPolicyProvider
{
    public Task<SecurityPolicy> GetPolicyAsync() =>
        Task.FromResult(policy ?? SecurityPolicy.Default);
}

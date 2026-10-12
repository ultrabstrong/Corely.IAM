using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Models;

namespace Corely.IAM.UnitTests;

internal class FixedSecurityPolicyProvider(SecurityPolicy policy) : ISecurityPolicyProvider
{
    public static FixedSecurityPolicyProvider From(SecurityOptions options) =>
        new(options.ToSecurityPolicy(SecurityPolicy.Default.ToPasswordValidationOptions()));

    public Task<SecurityPolicy> GetPolicyAsync() => Task.FromResult(policy);
}

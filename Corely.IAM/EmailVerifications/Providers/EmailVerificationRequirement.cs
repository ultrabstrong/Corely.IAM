using Corely.Common.Extensions;
using Corely.IAM.Platform.Providers;

namespace Corely.IAM.EmailVerifications.Providers;

internal sealed class EmailVerificationRequirement(
    ISecurityPolicyProvider securityPolicyProvider,
    IEmailVerificationSender? sender = null
)
{
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));

    public IEmailVerificationSender? Sender { get; } = sender;

    public async Task<bool> AppliesAsync() =>
        Sender != null && (await _securityPolicyProvider.GetPolicyAsync()).RequireVerifiedEmail;
}

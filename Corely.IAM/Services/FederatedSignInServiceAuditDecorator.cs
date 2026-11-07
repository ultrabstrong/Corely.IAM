using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class FederatedSignInServiceAuditDecorator(
    IFederatedSignInService inner,
    IAuditProvider auditProvider
) : IFederatedSignInService
{
    private const string SERVICE = nameof(IFederatedSignInService);

    private readonly IFederatedSignInService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<IReadOnlyList<IdentityProviderOption>> ListIdentityProvidersAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Read, SecurityPolicy.RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            _inner.ListIdentityProvidersAsync,
            _ => AuditOutcome.Of(RetrieveResultCode.Success)
        );

    public Task<StartFederatedSignInResult> StartFederatedSignInAsync(
        StartFederatedSignInRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, User.RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.StartFederatedSignInAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<CompleteFederatedSignInResult> CompleteFederatedSignInAsync(
        CompleteFederatedSignInRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, User.RESOURCE_TYPE)
            {
                AccountId = request.AccountId ?? Guid.Empty,
            },
            () => _inner.CompleteFederatedSignInAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );
}

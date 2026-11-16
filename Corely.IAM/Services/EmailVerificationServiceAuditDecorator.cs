using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.EmailVerifications.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class EmailVerificationServiceAuditDecorator(
    IEmailVerificationService inner,
    IAuditProvider auditProvider
) : IEmailVerificationService
{
    private const string SERVICE = nameof(IEmailVerificationService);

    private readonly IEmailVerificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<RequestEmailVerificationResult> RequestEmailVerificationAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute),
            () => _inner.RequestEmailVerificationAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RequestEmailVerificationResult> RequestEmailVerificationForUserAsync(Guid userId) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute),
            () => _inner.RequestEmailVerificationForUserAsync(userId),
            r => AuditOutcome.Of(r.ResultCode, userId)
        );

    public Task<RequestEmailVerificationResult> ResendEmailVerificationAsync(
        string challengeToken
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute),
            () => _inner.ResendEmailVerificationAsync(challengeToken),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<GetEmailVerificationStatusResult> GetEmailVerificationStatusAsync(
        string challengeToken
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            () => _inner.GetEmailVerificationStatusAsync(challengeToken),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<VerifyEmailResult> VerifyEmailAsync(VerifyEmailRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.VerifyEmailAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, User.RESOURCE_TYPE) { AccountId = Guid.Empty };
}

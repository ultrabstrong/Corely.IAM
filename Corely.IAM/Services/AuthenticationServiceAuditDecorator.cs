using Corely.Common.Extensions;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class AuthenticationServiceAuditDecorator(
    IAuthenticationService inner,
    IAuditProvider auditProvider
) : IAuthenticationService
{
    private const string SERVICE = nameof(IAuthenticationService);

    private readonly IAuthenticationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<SignInResult> SignInAsync(SignInRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute) with
            {
                AccountId = request.AccountId ?? Guid.Empty,
                ActorUsername = request.Username,
            },
            () => _inner.SignInAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<SignInResult> SignInWithGoogleAsync(SignInWithGoogleRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute) with
            {
                AccountId = request.AccountId ?? Guid.Empty,
            },
            () => _inner.SignInWithGoogleAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<SignInResult> VerifyMfaAsync(VerifyMfaRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, PermissionConstants.USER_RESOURCE_TYPE),
            () => _inner.VerifyMfaAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RenewAuthTokenResult> RenewAuthTokenAsync(RenewAuthTokenRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Read, PermissionConstants.USER_RESOURCE_TYPE),
            () => _inner.RenewAuthTokenAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Execute, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.AccountId],
            },
            () => _inner.SwitchAccountAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<UserSession>> ListSessionsAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            () => _inner.ListSessionsAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> RevokeSessionAsync(RevokeSessionRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.RevokeSessionAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> RevokeOtherSessionsAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.RevokeOtherSessionsAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<bool> SignOutAsync(SignOutRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute),
            () => _inner.SignOutAsync(request),
            signedOut => new AuditOutcome(
                signedOut ? AuditConstants.SUCCESS_RESULT_CODE : AuditConstants.FAILED_RESULT_CODE
            )
        );

    public Task SignOutAllAsync() =>
        _auditProvider.RecordAsync(OnUser(AuthAction.Execute), () => _inner.SignOutAllAsync());

    public Task<UserAuthTokenValidationResultCode> AuthenticateWithTokenAsync(string authToken) =>
        _inner.AuthenticateWithTokenAsync(authToken);

    public void AuthenticateAsSystem(string deviceId) => _inner.AuthenticateAsSystem(deviceId);

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, PermissionConstants.USER_RESOURCE_TYPE) { AccountId = Guid.Empty };
}

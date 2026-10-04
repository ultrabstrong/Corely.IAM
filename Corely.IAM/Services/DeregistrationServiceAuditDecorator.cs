using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class DeregistrationServiceAuditDecorator(
    IDeregistrationService inner,
    IAuditProvider auditProvider
) : IDeregistrationService
{
    private const string SERVICE = nameof(IDeregistrationService);

    private readonly IDeregistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<DeregisterUserResult> DeregisterUserAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
                Detail = AuditDetail.DeletedUsername,
            },
            () => _inner.DeregisterUserAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterAccountResult> DeregisterAccountAsync(DeregisterAccountRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.AccountId],
                Detail = AuditDetail.DeletedAccountName,
            },
            () => _inner.DeregisterAccountAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.GroupId],
            },
            () => _inner.DeregisterGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.ROLE_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.RoleId],
            },
            () => _inner.DeregisterRoleAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.PERMISSION_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.PermissionId],
            },
            () => _inner.DeregisterPermissionAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.AccountId, request.UserId],
            },
            () => _inner.DeregisterUserFromAccountAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.GroupId, .. request.UserIds ?? []],
            },
            () => _inner.DeregisterUsersFromGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.GroupId, .. request.RoleIds ?? []],
            },
            () => _inner.DeregisterRolesFromGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.UserId, .. request.RoleIds ?? []],
            },
            () => _inner.DeregisterRolesFromUserAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ROLE_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.RoleId, .. request.PermissionIds ?? []],
            },
            () => _inner.DeregisterPermissionsFromRoleAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DeregisterBasicAuthResult> DeregisterBasicAuthAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Delete, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.DeregisterBasicAuthAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );
}

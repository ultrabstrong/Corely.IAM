using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class RegistrationServiceAuditDecorator(
    IRegistrationService inner,
    IAuditProvider auditProvider
) : IRegistrationService
{
    private const string SERVICE = nameof(IRegistrationService);

    private readonly IRegistrationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<RegisterUserResult> RegisterUserAsync(RegisterUserRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.RegisterUserAsync(request),
            r =>
                AuditOutcome.Of(r.ResultCode, r.CreatedUserId) with
                {
                    ActorUserId = r.CreatedUserId,
                }
        );

    public Task<RegisterUserWithGoogleResult> RegisterUserWithGoogleAsync(
        RegisterUserWithGoogleRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.RegisterUserWithGoogleAsync(request),
            r =>
                AuditOutcome.Of(r.ResultCode, r.CreatedUserId) with
                {
                    ActorUserId = r.CreatedUserId,
                }
        );

    public Task<RegisterAccountResult> RegisterAccountAsync(RegisterAccountRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.RegisterAccountAsync(request),
            r =>
                AuditOutcome.Of(r.ResultCode, r.CreatedAccountId) with
                {
                    AccountId = r.CreatedAccountId,
                }
        );

    public Task<RegisterGroupResult> RegisterGroupAsync(RegisterGroupRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
            },
            () => _inner.RegisterGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.CreatedGroupId)
        );

    public Task<RegisterRoleResult> RegisterRoleAsync(RegisterRoleRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.ROLE_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
            },
            () => _inner.RegisterRoleAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.CreatedRoleId)
        );

    public Task<RegisterPermissionResult> RegisterPermissionAsync(
        RegisterPermissionRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.PERMISSION_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
            },
            () => _inner.RegisterPermissionAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.CreatedPermissionId)
        );

    public Task<RegisterUserWithAccountResult> RegisterUserWithAccountAsync(
        RegisterUserWithAccountRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.AccountId, request.UserId],
            },
            () => _inner.RegisterUserWithAccountAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RegisterUsersWithGroupResult> RegisterUsersWithGroupAsync(
        RegisterUsersWithGroupRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.GroupId, .. request.UserIds ?? []],
            },
            () => _inner.RegisterUsersWithGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RegisterRolesWithGroupResult> RegisterRolesWithGroupAsync(
        RegisterRolesWithGroupRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.GROUP_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.GroupId, .. request.RoleIds ?? []],
            },
            () => _inner.RegisterRolesWithGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RegisterRolesWithUserResult> RegisterRolesWithUserAsync(
        RegisterRolesWithUserRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.UserId, .. request.RoleIds ?? []],
            },
            () => _inner.RegisterRolesWithUserAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RegisterPermissionsWithRoleResult> RegisterPermissionsWithRoleAsync(
        RegisterPermissionsWithRoleRequest request
    ) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.ROLE_RESOURCE_TYPE)
            {
                AccountId = request.AccountId,
                ResourceIds = [request.RoleId, .. request.PermissionIds ?? []],
            },
            () => _inner.RegisterPermissionsWithRoleAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<SetPasswordResult> SetPasswordAsync(SetPasswordRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.USER_RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.SetPasswordAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );
}

using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class ModificationServiceAuditDecorator(
    IModificationService inner,
    IAuditProvider auditProvider
) : IModificationService
{
    private const string SERVICE = nameof(IModificationService);

    private readonly IModificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request) =>
        _auditProvider.RecordAsync(
            Update(Account.RESOURCE_TYPE, request.AccountId, request.AccountId),
            () => _inner.ModifyAccountAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request) =>
        _auditProvider.RecordAsync(
            Update(User.RESOURCE_TYPE, Guid.Empty, request.UserId),
            () => _inner.ModifyUserAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request) =>
        _auditProvider.RecordAsync(
            Update(Group.RESOURCE_TYPE, request.AccountId, request.GroupId),
            () => _inner.ModifyGroupAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request) =>
        _auditProvider.RecordAsync(
            Update(Role.RESOURCE_TYPE, request.AccountId, request.RoleId),
            () => _inner.ModifyRoleAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> ModifyPermissionAsync(UpdatePermissionRequest request) =>
        _auditProvider.RecordAsync(
            Update(Permission.RESOURCE_TYPE, request.AccountId, request.PermissionId),
            () => _inner.ModifyPermissionAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> ModifyApiKeyAsync(UpdateApiKeyRequest request) =>
        _auditProvider.RecordAsync(
            Update(ApiKey.RESOURCE_TYPE, request.AccountId, request.ApiKeyId),
            () => _inner.ModifyApiKeyAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(
        RotateApiKeySecretRequest request
    ) =>
        _auditProvider.RecordAsync(
            Update(ApiKey.RESOURCE_TYPE, request.AccountId, request.ApiKeyId),
            () => _inner.RotateApiKeySecretAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request) =>
        _auditProvider.RecordAsync(
            Update(Account.RESOURCE_TYPE, request.AccountId, request.AccountId),
            () => _inner.RotateAccountKeyAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, User.RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.RotateCurrentUserKeyAsync(keyType),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyPasswordResult> ModifyPasswordAsync(ModifyPasswordRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, User.RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.ModifyPasswordAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall Update(string resourceType, Guid accountId, Guid resourceId) =>
        new(SERVICE, AuthAction.Update, resourceType)
        {
            AccountId = accountId,
            ResourceIds = [resourceId],
        };
}

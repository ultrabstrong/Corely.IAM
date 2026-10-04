using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class RetrievalServiceAuditDecorator(IRetrievalService inner, IAuditProvider auditProvider)
    : IRetrievalService
{
    private const string SERVICE = nameof(IRetrievalService);

    private readonly IRetrievalService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request
    ) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.PERMISSION_RESOURCE_TYPE) with
            {
                AccountId = request.AccountId,
            },
            () => _inner.ListPermissionsAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    ) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.PERMISSION_RESOURCE_TYPE) with
            {
                ResourceIds = [permissionId],
            },
            () => _inner.GetPermissionAsync(permissionId, hydrate),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<Group>> ListGroupsAsync(ListGroupsRequest request) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.GROUP_RESOURCE_TYPE) with
            {
                AccountId = request.AccountId,
            },
            () => _inner.ListGroupsAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<Group>> GetGroupAsync(Guid groupId, bool hydrate = false) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.GROUP_RESOURCE_TYPE) with
            {
                ResourceIds = [groupId],
            },
            () => _inner.GetGroupAsync(groupId, hydrate),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<Role>> ListRolesAsync(ListRolesRequest request) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.ROLE_RESOURCE_TYPE) with
            {
                AccountId = request.AccountId,
            },
            () => _inner.ListRolesAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.ROLE_RESOURCE_TYPE) with
            {
                ResourceIds = [roleId],
            },
            () => _inner.GetRoleAsync(roleId, hydrate),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<User>> ListUsersAsync(ListUsersRequest request) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.USER_RESOURCE_TYPE) with
            {
                AccountId = request.AccountId,
            },
            () => _inner.ListUsersAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.USER_RESOURCE_TYPE) with
            {
                ResourceIds = [userId],
            },
            () => _inner.GetUserAsync(userId, hydrate),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveListResult<Account>> ListAccountsAsync(ListAccountsRequest request) =>
        _auditProvider.RecordAsync(
            Read(PermissionConstants.ACCOUNT_RESOURCE_TYPE) with
            {
                AccountId = Guid.Empty,
            },
            () => _inner.ListAccountsAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    ) =>
        _auditProvider.RecordAsync(
            InAccount(accountId),
            () => _inner.GetAccountAsync(accountId, hydrate),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetAccountSymmetricEncryptionProviderAsync(Guid accountId) =>
        _auditProvider.RecordAsync(
            InAccount(accountId),
            () => _inner.GetAccountSymmetricEncryptionProviderAsync(accountId),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetAccountAsymmetricEncryptionProviderAsync(Guid accountId) =>
        _auditProvider.RecordAsync(
            InAccount(accountId),
            () => _inner.GetAccountAsymmetricEncryptionProviderAsync(accountId),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetAccountAsymmetricSignatureProviderAsync(Guid accountId) =>
        _auditProvider.RecordAsync(
            InAccount(accountId),
            () => _inner.GetAccountAsymmetricSignatureProviderAsync(accountId),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamSymmetricEncryptionProvider>
    > GetUserSymmetricEncryptionProviderAsync() =>
        _auditProvider.RecordAsync(
            OwnUser(),
            () => _inner.GetUserSymmetricEncryptionProviderAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamAsymmetricEncryptionProvider>
    > GetUserAsymmetricEncryptionProviderAsync() =>
        _auditProvider.RecordAsync(
            OwnUser(),
            () => _inner.GetUserAsymmetricEncryptionProviderAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<
        RetrieveSingleResult<IIamAsymmetricSignatureProvider>
    > GetUserAsymmetricSignatureProviderAsync() =>
        _auditProvider.RecordAsync(
            OwnUser(),
            () => _inner.GetUserAsymmetricSignatureProviderAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall Read(string resourceType) =>
        new(SERVICE, AuthAction.Read, resourceType);

    private static AuditCall InAccount(Guid accountId) =>
        Read(PermissionConstants.ACCOUNT_RESOURCE_TYPE) with
        {
            AccountId = accountId,
            ResourceIds = [accountId],
        };

    private static AuditCall OwnUser() =>
        Read(PermissionConstants.USER_RESOURCE_TYPE) with
        {
            AccountId = Guid.Empty,
        };
}

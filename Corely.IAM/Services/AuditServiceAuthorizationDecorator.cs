using Corely.Common.Extensions;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class AuditServiceAuthorizationDecorator(
    IAuditService inner,
    IAuthorizationProvider authorizationProvider,
    IAuditAccessProvider auditAccessProvider
) : IAuditService
{
    private const string UNAUTHORIZED = "Unauthorized";

    private readonly IAuditService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
    private readonly IAuditAccessProvider _auditAccessProvider = auditAccessProvider.ThrowIfNull(
        nameof(auditAccessProvider)
    );

    public Task<RetrieveListResult<AuditEntry>> ListEntriesAsync(ListAuditEntriesRequest request) =>
        _authorizationProvider.HasUserContext()
            ? _inner.ListEntriesAsync(request)
            : Task.FromResult(
                new RetrieveListResult<AuditEntry>(
                    RetrieveResultCode.UnauthorizedError,
                    UNAUTHORIZED,
                    null
                )
            );

    public Task<RetrieveSingleResult<AuditEntry>> GetEntryAsync(Guid entryId) =>
        _authorizationProvider.HasUserContext()
            ? _inner.GetEntryAsync(entryId)
            : Task.FromResult(
                new RetrieveSingleResult<AuditEntry>(
                    RetrieveResultCode.UnauthorizedError,
                    UNAUTHORIZED,
                    null,
                    null
                )
            );

    public Task<ExportAuditEntriesResult> ExportEntriesAsync(AuditEntryFilter filter) =>
        _authorizationProvider.HasUserContext()
            ? _inner.ExportEntriesAsync(filter)
            : Task.FromResult(
                new ExportAuditEntriesResult(
                    RetrieveResultCode.UnauthorizedError,
                    UNAUTHORIZED,
                    null,
                    0,
                    false
                )
            );

    public Task<ListAuditAccountsResult> ListAuditAccountsAsync(AuthAction action) =>
        _authorizationProvider.HasUserContext()
            ? _inner.ListAuditAccountsAsync(action)
            : Task.FromResult(
                new ListAuditAccountsResult(
                    RetrieveResultCode.UnauthorizedError,
                    UNAUTHORIZED,
                    [],
                    false
                )
            );

    public async Task<PurgeAuditEntriesResult> CountPurgeableEntriesAsync(
        PurgeAuditEntriesRequest request
    ) =>
        await CanPurgeAsync(request)
            ? await _inner.CountPurgeableEntriesAsync(request)
            : new PurgeAuditEntriesResult(
                PurgeAuditEntriesResultCode.UnauthorizedError,
                UNAUTHORIZED,
                0
            );

    public async Task<PurgeAuditEntriesResult> PurgeEntriesAsync(
        PurgeAuditEntriesRequest request
    ) =>
        await CanPurgeAsync(request)
            ? await _inner.PurgeEntriesAsync(request)
            : new PurgeAuditEntriesResult(
                PurgeAuditEntriesResultCode.UnauthorizedError,
                UNAUTHORIZED,
                0
            );

    public async Task<GetAccountAuditSettingsResult> GetAccountSettingsAsync(Guid accountId) =>
        await IsAuthorizedForAccountSettingsAsync(AuthAction.Read, accountId)
            ? await _inner.GetAccountSettingsAsync(accountId)
            : new GetAccountAuditSettingsResult(
                RetrieveResultCode.UnauthorizedError,
                UNAUTHORIZED,
                null,
                AuditActions.None,
                0
            );

    public async Task<ModifyResult> UpdateAccountSettingsAsync(
        UpdateAccountAuditSettingsRequest request
    ) =>
        await IsAuthorizedForAccountSettingsAsync(AuthAction.Update, request.AccountId)
            ? await _inner.UpdateAccountSettingsAsync(request)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, UNAUTHORIZED);

    public async Task<GetPlatformSettingsResult> GetPlatformSettingsAsync() =>
        await _auditAccessProvider.HoldsInPlatformAccountAsync(
            AuthAction.Read,
            AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
        )
            ? await _inner.GetPlatformSettingsAsync()
            : new GetPlatformSettingsResult(
                RetrieveResultCode.UnauthorizedError,
                UNAUTHORIZED,
                null
            );

    public async Task<ModifyResult> UpdatePlatformSettingsAsync(PlatformSettings settings) =>
        await _auditAccessProvider.HoldsInPlatformAccountAsync(
            AuthAction.Update,
            AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE
        )
            ? await _inner.UpdatePlatformSettingsAsync(settings)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, UNAUTHORIZED);

    public Task<DeleteExpiredAuditEntriesResult> DeleteExpiredEntriesAsync() =>
        _authorizationProvider.HasUserContext() && !_authorizationProvider.IsNonSystemUserContext()
            ? _inner.DeleteExpiredEntriesAsync()
            : Task.FromResult(
                new DeleteExpiredAuditEntriesResult(
                    DeleteExpiredAuditEntriesResultCode.UnauthorizedError,
                    "Deleting expired audit entries requires system context",
                    0
                )
            );

    private async Task<bool> CanPurgeAsync(PurgeAuditEntriesRequest request) =>
        (await _auditAccessProvider.GetAccessAsync(AuthAction.Delete)).CanReach(request.AccountId);

    private async Task<bool> IsAuthorizedForAccountSettingsAsync(
        AuthAction action,
        Guid accountId
    ) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            action,
            AuditConstants.AUDIT_SETTINGS_RESOURCE_TYPE
        );
}

using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Mappers;
using Corely.IAM.Audits.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Audits.Providers;

internal class AuditProvider(
    IUserContextProvider userContextProvider,
    IAuditPolicy auditPolicy,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<UserEntity> userRepo,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<AuditOptions> auditOptions,
    ILogger<AuditProvider> logger
) : IAuditProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IAuditPolicy _auditPolicy = auditPolicy.ThrowIfNull(nameof(auditPolicy));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory.ThrowIfNull(
        nameof(scopeFactory)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly string _source = auditOptions
        .ThrowIfNull(nameof(auditOptions))
        .Value.SourceName()
        .Truncated(AuditConstants.SOURCE_MAX_LENGTH);
    private readonly ILogger<AuditProvider> _logger = logger.ThrowIfNull(nameof(logger));

    public async Task<TResult> RecordAsync<TResult>(
        AuditCall call,
        Func<Task<TResult>> operation,
        Func<TResult, AuditOutcome> outcome,
        string operationName = ""
    )
    {
        var pending = await BeginAsync(call, operationName);
        TResult result;
        try
        {
            result = await operation();
        }
        catch
        {
            await CompleteAsync(pending, () => new AuditOutcome(AuditConstants.FAULT_RESULT_CODE));
            throw;
        }

        await CompleteAsync(pending, () => outcome(result));
        return result;
    }

    public Task RecordAsync(AuditCall call, Func<Task> operation, string operationName = "") =>
        RecordAsync(
            call,
            async () =>
            {
                await operation();
                return true;
            },
            _ => new AuditOutcome(AuditConstants.SUCCESS_RESULT_CODE),
            operationName
        );

    private async Task<PendingAudit> BeginAsync(AuditCall call, string operation)
    {
        var startedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var before = _userContextProvider.GetUserContext();
        var accountId =
            call.AccountId == Guid.Empty ? null : call.AccountId ?? before?.CurrentAccount?.Id;
        var pending = new PendingAudit(call, operation, startedUtc, before, accountId, false, null);

        try
        {
            var memberBefore =
                accountId is { } id
                && before is { IsSystemContext: false, User: { } user }
                && await MayRecordForAccountAsync(call, operation, id)
                && await IsMemberAsync(user.Id, id);

            return pending with
            {
                MemberBefore = memberBefore,
                Details = await DetailsAsync(call, before, accountId),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Audit could not prepare the entry for {Service}.{Operation}; it is still attempted after the call",
                call.Service,
                operation
            );
            return pending;
        }
    }

    private async Task CompleteAsync(PendingAudit pending, Func<AuditOutcome> outcome)
    {
        var call = pending.Call;
        try
        {
            var after = _userContextProvider.GetUserContext();
            var context = pending.Context(after);
            var described = outcome();
            var accountId = NonEmpty(described.AccountId) ?? pending.AccountIdAfter(after);
            var isSystem = context?.IsSystemContext == true;
            var actorUserId = isSystem
                ? null
                : pending.Before?.User?.Id
                    ?? after?.User?.Id
                    ?? NonEmpty(described.ActorUserId)
                    ?? await FindUserIdAsync(call.ActorUsername);

            var alwaysRecorded = _auditPolicy.IsAlwaysRecorded(call.Service, pending.Operation);

            AuditCohort cohort;
            if (isSystem)
                cohort = AuditCohort.SystemContext;
            else if (accountId is null || actorUserId is null)
                cohort = AuditCohort.Accountless;
            else
            {
                if (
                    !alwaysRecorded
                    && !await MayRecordForAccountAsync(call, pending.Operation, accountId.Value)
                )
                    return;

                cohort =
                    pending.MemberBefore || await IsMemberAsync(actorUserId.Value, accountId.Value)
                        ? AuditCohort.AccountMember
                        : AuditCohort.PlatformMember;
            }

            if (
                !alwaysRecorded
                && !await _auditPolicy.IsRecordedAsync(cohort, accountId, call.Action)
            )
                return;

            IEnumerable<Guid> resourceIds = [.. call.ResourceIds, .. described.ResourceIds];
            if (call.Detail == AuditDetail.DeletedUsername && actorUserId is { } deletedUserId)
                resourceIds = resourceIds.Append(deletedUserId);

            var entry = new AuditEntryEntity
            {
                Id = Guid.CreateVersion7(),
                OccurredUtc = pending.StartedUtc,
                ActorUserId = actorUserId,
                Cohort = cohort,
                AccountId = accountId,
                Source = _source,
                Service = call.Service.Truncated(AuditConstants.SERVICE_MAX_LENGTH),
                Operation = pending.Operation.Truncated(AuditConstants.OPERATION_MAX_LENGTH),
                Action = call.Action,
                ResourceType = call.ResourceType.Truncated(AuditConstants.RESOURCE_TYPE_MAX_LENGTH),
                ResourceIds = resourceIds.ToResourceIdsColumn(),
                ResultCode = described.ResultCode.Truncated(AuditConstants.RESULT_CODE_MAX_LENGTH),
                Details = pending.Details?.Truncated(AuditConstants.DETAILS_MAX_LENGTH),
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<IRepo<AuditEntryEntity>>()
                .CreateAsync(entry, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Audit entry was not written for {Service}.{Operation}; the call's result is returned unchanged",
                call.Service,
                pending.Operation
            );
        }
    }

    private async Task<bool> MayRecordForAccountAsync(
        AuditCall call,
        string operation,
        Guid accountId
    ) =>
        _auditPolicy.IsAlwaysRecorded(call.Service, operation)
        || await _auditPolicy.IsRecordedAsync(AuditCohort.AccountMember, accountId, call.Action)
        || await _auditPolicy.IsRecordedAsync(AuditCohort.PlatformMember, accountId, call.Action);

    private static Guid? NonEmpty(Guid? id) => id == Guid.Empty ? null : id;

    private Task<bool> IsMemberAsync(Guid userId, Guid accountId) =>
        _accountRepo.AnyAsync(a => a.Id == accountId && a.Users!.Any(u => u.Id == userId));

    private async Task<Guid?> FindUserIdAsync(string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? null
            : (await _userRepo.GetAsync(u => u.Username == username))?.Id;

    private async Task<string?> DetailsAsync(
        AuditCall call,
        UserContext? before,
        Guid? accountId
    ) =>
        call.Detail switch
        {
            AuditDetail.DeletedUsername => before?.User?.Username,
            AuditDetail.DeletedAccountName when accountId is { } id => (
                await _accountRepo.GetAsync(a => a.Id == id)
            )?.AccountName,
            _ => null,
        };
}

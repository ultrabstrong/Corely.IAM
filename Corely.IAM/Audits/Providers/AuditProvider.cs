using System.Runtime.CompilerServices;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Entities;
using Corely.IAM.Audits.Mappers;
using Corely.IAM.Audits.Models;
using Corely.IAM.Extensions;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Audits.Providers;

internal class AuditProvider(
    IUserContextProvider userContextProvider,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<AuditOptions> auditOptions,
    ILogger<AuditProvider> logger
) : IAuditProvider
{
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

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
        [CallerMemberName] string operationName = ""
    )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var pending = await BeginAsync(services, call, operationName);
        TResult result;
        try
        {
            result = await operation();
        }
        catch
        {
            await CompleteAsync(
                services,
                pending,
                () => new AuditOutcome(AuditConstants.FAULT_RESULT_CODE)
            );
            throw;
        }

        await CompleteAsync(services, pending, () => outcome(result));
        return result;
    }

    public Task RecordAsync(
        AuditCall call,
        Func<Task> operation,
        [CallerMemberName] string operationName = ""
    ) =>
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

    private async Task<PendingAudit> BeginAsync(
        IServiceProvider services,
        AuditCall call,
        string operation
    )
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
                && await MayRecordForAccountAsync(services, call, operation, id)
                && await IsMemberAsync(services, user.Id, id);

            return pending with
            {
                MemberBefore = memberBefore,
                Details = await DetailsAsync(services, call, before, accountId),
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

    private async Task CompleteAsync(
        IServiceProvider services,
        PendingAudit pending,
        Func<AuditOutcome> outcome
    )
    {
        var call = pending.Call;
        try
        {
            var policy = services.GetRequiredService<IAuditPolicy>();
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
                    ?? await FindUserIdAsync(services, call.ActorUsername)
                    ?? await FindChallengeUserIdAsync(services, call.ActorMfaChallengeToken);

            var alwaysRecorded = policy.IsAlwaysRecorded(call.Service, pending.Operation);

            AuditCohort cohort;
            if (isSystem)
                cohort = AuditCohort.SystemContext;
            else if (accountId is null || actorUserId is null)
                cohort = AuditCohort.Accountless;
            else
            {
                if (
                    !alwaysRecorded
                    && !await MayRecordForAccountAsync(
                        services,
                        call,
                        pending.Operation,
                        accountId.Value
                    )
                )
                    return;

                cohort =
                    pending.MemberBefore
                    || await IsMemberAsync(services, actorUserId.Value, accountId.Value)
                        ? AuditCohort.AccountMember
                        : AuditCohort.PlatformMember;
            }

            if (!alwaysRecorded && !await policy.IsRecordedAsync(cohort, accountId, call.Action))
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

            await services
                .GetRequiredService<IRepo<AuditEntryEntity>>()
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

    private static async Task<bool> MayRecordForAccountAsync(
        IServiceProvider services,
        AuditCall call,
        string operation,
        Guid accountId
    )
    {
        var policy = services.GetRequiredService<IAuditPolicy>();
        return policy.IsAlwaysRecorded(call.Service, operation)
            || await policy.IsRecordedAsync(AuditCohort.AccountMember, accountId, call.Action)
            || await policy.IsRecordedAsync(AuditCohort.PlatformMember, accountId, call.Action);
    }

    private static Guid? NonEmpty(Guid? id) => id == Guid.Empty ? null : id;

    private static Task<bool> IsMemberAsync(
        IServiceProvider services,
        Guid userId,
        Guid accountId
    ) =>
        services
            .GetRequiredService<IReadonlyRepo<AccountEntity>>()
            .AnyAsync(a => a.Id == accountId && a.Users!.Any(u => u.Id == userId));

    private static async Task<Guid?> FindUserIdAsync(IServiceProvider services, string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? null
            : (
                await services
                    .GetRequiredService<IReadonlyRepo<UserEntity>>()
                    .GetAsync(u => u.Username == username)
            )?.Id;

    private static async Task<Guid?> FindChallengeUserIdAsync(
        IServiceProvider services,
        string? challengeToken
    ) =>
        string.IsNullOrWhiteSpace(challengeToken)
            ? null
            : (
                await services
                    .GetRequiredService<IReadonlyRepo<MfaChallengeEntity>>()
                    .GetAsync(c => c.ChallengeToken == challengeToken)
            )?.UserId;

    private static async Task<string?> DetailsAsync(
        IServiceProvider services,
        AuditCall call,
        UserContext? before,
        Guid? accountId
    ) =>
        call.Detail switch
        {
            AuditDetail.DeletedUsername => before?.User?.Username,
            AuditDetail.DeletedAccountName when accountId is { } id => (
                await services
                    .GetRequiredService<IReadonlyRepo<AccountEntity>>()
                    .GetAsync(a => a.Id == id)
            )?.AccountName,
            _ => null,
        };
}

# Auditing

Records who did what, in which account, and whether it was allowed. Every service method passes
through an audit decorator; the platform settings and each account's settings decide what is actually
written.

## Features

- **Every service**: each IAM service has an audit decorator between telemetry and authorization, so refused calls are recorded with their refusal code
- **Configured, not coded**: the platform sets the limits, each account chooses what is recorded for its members and for platform members
- **No request contents**: entries hold ids, the action, the result code and nothing from a request body
- **Append only**: nothing updates an entry; the only delete is a purge of one account's entries or the retention cleanup
- **Best effort**: the entry is written after the call; a failed write is logged as an error and the call's result is returned unchanged
- **Cross-account reading**: one list covers every account the caller may read plus their own activity

## Entries

| Column | Holds |
|--------|-------|
| `OccurredUtc` | When the call started, from `TimeProvider` |
| `ActorUserId` | Who acted. Empty for system context, and for a failed sign in with an unknown username |
| `Cohort` | `AccountMember`, `PlatformMember`, `Accountless` or `SystemContext` |
| `AccountId` | The account acted in. Empty for signing in, setting a password, MFA, sessions and key rotation of a user |
| `Source` | The app that wrote it, from `AuditOptions:Source` |
| `Service`, `Operation` | The service interface and method, such as `IRegistrationService` and `RegisterGroupAsync` |
| `Action`, `ResourceType`, `ResourceIds` | The CRUDX action and what it was on. A created resource's id comes from the result |
| `ResultCode` | The result code returned, refusals included, `Fault` when the call threw |
| `Details` | The username of a deleted user, or the name of a deleted account. Nothing else writes it |

Entries refer to users and accounts by id with no foreign key, so they outlive both. A deleted user's
or account's name is read back from its deletion entry, which is why those two deletions are always
recorded.

## Cohorts

| Cohort | Who | Configured by | Default |
|--------|-----|---------------|---------|
| Account members | A user acting in an account they belong to | The account | Create, Update, Delete |
| Platform members | A user acting in an account they do not belong to, through the [platform account](platform.md) | The account | Create, Update, Delete |
| Outside any account | Signing in, setting a password, MFA, sessions | The platform | Create, Update, Delete, Execute |
| System context | Headless processes | The platform | Nothing |

A user who belonged to the account at any point of the call counts as a member, so leaving an account
or accepting an invitation is recorded as an account member.

## Settings

Platform settings are one row, read and changed by holders of `platform_settings` in the platform
account:

| Setting | Meaning |
|---------|---------|
| `AuditEnabled` | Off records nothing anywhere except deleting a user or an account |
| `AuditMaxRetentionDays` | The longest any account keeps entries. Also the retention outside any account, for system context and for deleted accounts |
| `AuditAllowedActions` | The actions any account may record |
| `SystemContextActions` | What is recorded for system context |
| `AccountlessActions` | What is recorded outside any account |

Account settings are one row per account, read and changed with `audit_settings`. An account with no
row uses the defaults.

```csharp
await auditService.UpdateAccountSettingsAsync(new UpdateAccountAuditSettingsRequest(
    accountId,
    AccountMemberActions: AuditActions.Create | AuditActions.Update | AuditActions.Delete,
    PlatformMemberActions: AuditActions.All,
    RetentionDays: 180));
```

The effective setting is worked out when it is used: the account's actions within the platform's
allowed actions, its retention within the platform's maximum. Lowering a platform limit rewrites no
account row, so raising it again brings each account's own choice back. An account that switches a
cohort off, platform members included, gets nothing recorded for it.

Settings are cached per instance for `AuditOptions:SettingsCacheTtlSeconds` (default 30). A change made
through `IAuditService` applies at once on that instance and on others when their cache expires.

## Reading

`IAuditService.ListEntriesAsync` returns, newest first:

- entries in every account where the caller holds Read on `audit`;
- every entry where the caller is the actor;
- everything, entries outside any account and system context included, for a caller holding Read on `audit` in the platform account, or under system context.

```csharp
var result = await auditService.ListEntriesAsync(new ListAuditEntriesRequest(
    new AuditEntryFilter
    {
        AccountId = accountId,
        Action = AuthAction.Delete,
        FromUtc = DateTime.UtcNow.AddDays(-7),
        IncludePlatformMembers = false,
    },
    Skip: 0,
    Take: 50));
```

Each entry carries `ActorName` and `AccountName`, read from Users and Accounts or from the deletion
entry once they are gone. `ExportEntriesAsync` runs the same query as CSV, stops at 10,000 entries and
sets `Truncated` when the filter matches more. `ListAuditAccountsAsync(AuthAction.Read)` lists the
accounts to offer in a filter.

## Purging

A purge removes one account's entries, all of them or those older than a date. It needs Delete on
`audit` in that account. Entries outside any account (`AccountId` of `null`) need Delete on `audit` in
the platform account. Single entries cannot be removed.

```csharp
var preview = await auditService.CountPurgeableEntriesAsync(new PurgeAuditEntriesRequest(accountId, cutoff));
var purged = await auditService.PurgeEntriesAsync(new PurgeAuditEntriesRequest(accountId, cutoff));
```

Switching auditing off and purging are separate: turning `AuditEnabled` off keeps existing entries
until they age out.

## Retention cleanup

IAM ships the operation; the app ships the schedule. `DeleteExpiredEntriesAsync` deletes each account's
entries older than its effective retention, and everything older than the platform maximum. It runs
only under system context and is safe to run twice.

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
scope.ServiceProvider.GetRequiredService<IAuthenticationService>().AuthenticateAsSystem("audit-cleanup");
var result = await scope.ServiceProvider.GetRequiredService<IAuditService>().DeleteExpiredEntriesAsync();
```

Call it daily from whatever schedules work in the app: a timer triggered function, or a hosted service
with a `PeriodicTimer` (the WebApp's `AuditCleanupService`).

## Auditing host services

A host audits its own services the same way IAM does: a hand-written decorator per service interface,
each method one call to `IAuditProvider`. Register it between telemetry and authorization.

```csharp
internal class InvoiceServiceAuditDecorator(IInvoiceService inner, IAuditProvider auditProvider)
    : IInvoiceService
{
    public Task<CreateInvoiceResult> CreateInvoiceAsync(CreateInvoiceRequest request) =>
        auditProvider.RecordAsync(
            new AuditCall(nameof(IInvoiceService), AuthAction.Create, "invoice")
            {
                AccountId = request.AccountId,
            },
            () => inner.CreateInvoiceAsync(request),
            r => AuditOutcome.Of(r.ResultCode, r.CreatedInvoiceId));
}
```

`AuditCall.AccountId` left `null` uses the caller's current account; `Guid.Empty` records the call
outside any account. The operation name comes from the calling method.

## Configuration

```json
"AuditOptions": {
  "Source": "portal",
  "SettingsCacheTtlSeconds": 30
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `Source` | The entry assembly's name | Written to every entry so entries from several apps can be told apart |
| `SettingsCacheTtlSeconds` | 30 | How long platform and account settings are cached per instance |

## Notes

- `AuthenticateWithTokenAsync` and `AuthenticateAsSystem` are not recorded. They set the context that every recorded call after them carries.
- Reads are off by default, so the busiest calls cost a cache lookup and no write.
- Reading the audit log is itself a Read on `audit`, recorded only where Reads are recorded.
- The Owner role gets Read and Delete on `audit` and Read and Update on `audit_settings` as
  [owner defaults](resource-types.md#owner-defaults), so only in accounts created after these types are
  registered. Until a host grants them in an existing account, under system context, that account's
  owners see only their own entries and cannot change its settings.

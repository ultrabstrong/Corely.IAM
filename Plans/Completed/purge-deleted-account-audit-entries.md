# Purge a deleted account's audit entries

**Status: done.** Released in Corely.IAM 4.0.0 and Corely.IAM.Web 4.0.0.

## As built

- `ListAuditAccountsResult` gains `DeletedAccounts`. `ListAuditAccountsAsync` fills it only when the
  viewer reaches everything for that action: the distinct account ids in the entries that have no
  Accounts row, named from their deletion entry's `Details`, or by id without one, ordered by name.
- The live account ids are compared in memory rather than sent to the database as a list, so a
  platform with many accounts never builds a large `IN` clause.
- Purging needed no change: `AuditAccess.CanReach` already lets everything reach any id.
- Both the purge modal and the audit page's account filter show them in a "Deleted accounts"
  `<optgroup>`.
- Tests: `AuditServiceTests` for the list with and without access to everything, and bUnit
  `AuditPurgeModalTests` for the group and for reviewing a deleted account's count.

## The problem

Audit entries outlive their account on purpose, and age out under the platform maximum. Until then
there is no way to purge them from the portal: the purge modal's account list comes from
`IAuditService.ListAuditAccountsAsync`, which reads the Accounts table, so a deleted account is not
offered. Only the API (`PurgeEntriesAsync` with the deleted account's id) can remove them.

## What is wanted

A platform member who may purge everything can also purge a deleted account's entries from the purge
modal, all of them or those older than a date, like any other account.

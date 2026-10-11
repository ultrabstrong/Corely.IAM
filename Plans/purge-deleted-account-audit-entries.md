# Purge a deleted account's audit entries

**Status: parked.** Recorded from auditing's build notes (`Completed/auditing.md`); not planned for any
release.

## The problem

Audit entries outlive their account on purpose, and age out under the platform maximum. Until then
there is no way to purge them from the portal: the purge modal's account list comes from
`IAuditService.ListAuditAccountsAsync`, which reads the Accounts table, so a deleted account is not
offered. Only the API (`PurgeEntriesAsync` with the deleted account's id) can remove them.

## What is wanted

A platform member who may purge everything can also purge a deleted account's entries from the purge
modal, all of them or those older than a date, like any other account.

## Starting points, not decisions

- **Who may.** A deleted account has no owners and no permissions left, so only a platform member
  holding Delete on `audit` in the platform account (the "everything" reach) can be offered it.
- **Finding them.** The account ids that have entries but no Accounts row. The account's name comes
  from its deletion entry's `Details`, as the audit page already does for names.
- **Showing them.** A separate group in the modal's account list ("Deleted accounts"), so a deleted
  account is never mistaken for a live one.
- The audit page's own account filter has the same gap and could take the same list.

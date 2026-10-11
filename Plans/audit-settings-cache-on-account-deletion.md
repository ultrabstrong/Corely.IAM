# Audit settings cache on account deletion

**Status: parked.** Recorded from auditing's build notes (`Completed/auditing.md`); low impact, not
planned for any release.

## The problem

`AuditSettingsCache` keeps each account's audit settings for `AuditOptions.SettingsCacheTtlSeconds`
(30 by default). Saving an account's settings clears its entry (`InvalidateAccount`), but deleting the
account does not. Until the entry expires, that instance still answers with the deleted account's own
settings.

A deleted account's entries are meant to age out under the platform maximum, since its settings went
with it. If the retention cleanup runs on the same instance within the cache window after the
deletion, it uses the account's own retention instead. The window is 30 seconds against a daily
cleanup, so this is close to theoretical; the next cleanup applies the right rule.

The entry also stays in the dictionary until the process restarts, though it is never read as fresh
again.

## What is wanted

Deleting an account clears its cached audit settings at once, on the instance that deleted it.

## Starting points, not decisions

- `DeregisterAccountAsync` (or the account processor behind it) calls
  `AuditSettingsCache.InvalidateAccount` after a successful delete, the way `AuditService` does after
  saving settings.
- Other instances still hold their own entry until it expires; the cache is per instance by design,
  and 30 seconds is the accepted staleness everywhere else.
- A test: with the clock frozen inside the window, delete an account and confirm the policy reports
  the platform maximum for it.

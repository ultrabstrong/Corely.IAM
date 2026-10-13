# Audit settings cache on account deletion

**Status: done.** Released in Corely.IAM 4.0.0.

## As built

`DeregistrationService.DeregisterAccountAsync` calls `AuditSettingsCache.InvalidateAccount` after a
successful delete, as `AuditService` does after saving settings. A failed delete leaves the entry.
Other instances still hold theirs until it expires, the accepted staleness everywhere else.
`DeregistrationServiceTests.DeregisterAccount_ClearsCachedAuditSettings_OnlyWhenDeleted` covers both.

## The problem

`AuditSettingsCache` keeps each account's audit settings for `AuditOptions.SettingsCacheTtlSeconds`
(30 by default). Saving an account's settings clears its entry (`InvalidateAccount`), but deleting the
account does not. Until the entry expires, that instance still answers with the deleted account's own
settings.

A deleted account's entries are meant to age out under the platform maximum, since its settings went
with it. If the retention cleanup runs on the same instance within the cache window after the
deletion, it uses the account's own retention instead. The window is 30 seconds against a daily
cleanup, so this is close to theoretical; the next cleanup applies the right rule.

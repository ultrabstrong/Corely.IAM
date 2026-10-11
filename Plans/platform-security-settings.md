# Security policy in platform settings

**Status: proposed.** Not scheduled for a release.

## Why

Password rules, lockout and token lifetimes are policy, but today they are app configuration
(`SecurityOptions`, `PasswordValidationOptions`). Changing one means editing `appsettings.json` and
restarting every instance. The platform account already has a settings row that platform members
edit at runtime (`PlatformSettingsEntity`, `platform_settings`, cached per instance by
`AuditSettingsCache`), so policy could live there too.

`DESIGN-DECISIONS.md` rejected account-scoped password rules because a password is set before the
user belongs to any account, and one user can belong to several. Platform rules do not have that
problem: there is one platform, and its rules apply to every user, as `PasswordValidationOptions`
does now.

## What moves, what stays

Moves: rules an operator tunes for the whole user base, read per request.

| Setting | Today |
|---------|-------|
| Password minimum length and complexity | `PasswordValidationOptions` |
| `MaxLoginAttempts`, `LockoutCooldownSeconds` | `SecurityOptions` |
| `AuthTokenTtlSeconds`, `AuthSessionTtlSeconds` | `SecurityOptions` |
| `MfaChallengeTimeoutSeconds` | `SecurityOptions` |
| `TotpRecoveryCodeCount` | `SecurityOptions` |

Stays in configuration: anything that identifies the deployment, is needed before the database can
be read, or tunes the process rather than the policy.

| Setting | Why it stays |
|---------|--------------|
| `GoogleClientId` | Deployment identity, paired with the host's Google console setup |
| `PermissionCacheTtlSeconds`, `AuditOptions.SettingsCacheTtlSeconds` | Process tuning; the second is what reading the settings depends on |
| `AuditOptions.Source` | Names the process |
| System keys, connection strings | Needed before anything is read |
| `IAMWebOptions.ForgotPasswordPath` | Host routing |

New policy that fits the same row once it exists, not part of this plan: require TOTP for every user,
or for platform members only.

## Starting points, not decisions

- **One row or a second one.** Add columns to `PlatformSettingsEntity`, or a separate security row.
  One row keeps one cache and one edit page; a second keeps audit and security changes apart in the
  audit log.
- **Configuration as the default.** Hosts with no platform account (the users only demo) still need
  values. Simplest: code defaults, the platform row overrides, configuration no longer binds the
  moved settings. Keeping configuration as a middle layer means three sources to explain.
- **Bounds.** A platform member must not set `MaxLoginAttempts` to 0 or a token lifetime of a year.
  Each setting needs a floor and a ceiling, enforced by a validator on update.
- **Existing passwords.** Raising the minimum length applies at the next set or change, never to a
  stored password. Whether to flag users whose password predates the rule is a separate question.
- **Staleness.** Instances pick up a change within the settings cache window (30 seconds), the same as
  audit settings.
- **Breaking.** Removing the configuration binding is a major version. 4.0 is not yet released, so
  this could ride along with it or wait for 5.0.
- **Portal.** The platform settings page gains a security section.

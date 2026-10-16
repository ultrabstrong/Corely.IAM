# Audit

## AuditLog (`/audit`)

The audit log across every account the viewer may read, plus the viewer's own activity. It is not
tied to the current account, so it works like Profile: it opens with or without one. Linked from the
navigation bar for every signed in user.

**Base class**: `EntityListPageBase<AuditEntry>`

**Features:**

### Filters
- **Account**: the accounts from `ListAuditAccountsAsync(AuthAction.Read)`, with its `DeletedAccounts` in a separate "Deleted accounts" group
- **User id**, **action**, **resource type** (from `IResourceTypeRegistry`), **result** and a **from/to** date range (UTC days)
- **Include platform members**: on by default; off hides entries in the `PlatformMember` cohort

### Table
- When, actor (with a Platform member badge), account, operation with its details and source, action, resource type and ids, result
- Names of deleted users and accounts come from their deletion entries
- Paginated, 25 per page, newest first

### Export CSV
- Downloads what the filters show through `ExportEntriesAsync`, using `js/file-download.js` (imported by the page, nothing for the host to add)
- Stops at 10,000 entries and says so, suggesting a narrower date range

### Purge
- Shown only when `ListAuditAccountsAsync(AuthAction.Delete)` returns an account or everything
- `AuditPurgeModal`: choose an account (and, for a platform member holding Delete on `audit` in the platform account, the entries outside any account and from system context, and the deleted accounts in their own group), then everything or everything older than a date
- Review shows the count about to go (`CountPurgeableEntriesAsync`) before the purge runs

**Authorization:** `IAuditService` scopes the list itself; the page shows whatever it returns.

## Platform settings (`/platform-settings`)

The platform settings, linked from the account menu while the platform account is the current
account and the user can read at least one area. Each area is a page with its own resource type,
and the menu on the left lists only the pages the user holds Read on. `/platform-settings` opens the
first of them, or says there is none. A page needs Read on its type held in the platform account,
and anyone else sees an access message. **Edit** is shown with Update on the type, and each page
saves on its own.

| Page | Type |
|------|------|
| Audit | `platform_audit_settings` |
| Security | `platform_security_settings` |

### Adding a page

A host or a library adds its own area through `IAMWebOptions.PlatformSettingsPages`. Each entry
names the page's title, path, Bootstrap icon and the resource type it needs; the host registers that
type with no owner actions and checks it in its own service. Pages appear after IAM's, in the order
they are added, and each layer adds its own without knowing about the others:

```csharp
// an app
builder.Services.AddIAMWeb(o =>
    o.PlatformSettingsPages.Add(
        new("SFTP", "/platform-settings/sftp", "bi-hdd-network", "app_sftp_settings")));

// a library's own setup method
services.Configure<IAMWebOptions>(o =>
    o.PlatformSettingsPages.Add(
        new("Billing", "/platform-settings/billing", "bi-receipt", "billing_settings")));
```

The page wraps its content in `PlatformSettingsFrame`, as IAM's pages do, so it shares the header
and the menu:

```razor
@page "/platform-settings/sftp"

<PlatformSettingsFrame>
    ...
</PlatformSettingsFrame>
```

### PlatformAuditSettingsPage (`/platform-settings/audit`)

Loads through `IAuditService.GetPlatformSettingsAsync`.

- **Auditing** on or off, **maximum retention**, the **actions accounts may record**, and what is recorded for **system context** and **outside any account**

### PlatformSecuritySettingsPage (`/platform-settings/security`)

Loads through `IPlatformService.GetSecuritySettingsAsync` and saves through
`UpdateSecuritySettingsAsync`.

- **Passwords:** minimum and maximum length, and the characters a password must contain. The minimum
  length shows that 8 or more is recommended
- **Sign in:** failed sign ins before lockout, lockout length, token lifetime, session lifetime
- **Two factor:** code prompt timeout, recovery codes per user
- Each number shows its range; a save outside it shows the refusal

See [Accounts](accounts.md) for the account's own audit settings, and Corely.IAM's
[Auditing](../../../Corely.IAM/Docs/auditing.md) docs for what each setting means.

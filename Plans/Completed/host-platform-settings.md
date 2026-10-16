# Plan: Platform settings, one permission per area, open to hosts

**Status: done.** Built for the unreleased Corely.IAM 5.0.0 and Corely.IAM.Web 6.0.0.

## As built

Where the build differs from the plan below:

- **IAM's own two pages are the list's first entries,** not a separate hard coded pair:
  `IAMWebOptions.PlatformSettingsPages` starts with Audit and Security, and hosts append to it. One
  list feeds the menu, the redirect and the nav link.
- **The readable pages are `IAMWebOptions.ReadablePlatformSettingsPagesAsync`,** an internal member
  of the options, so the menu, the redirect and the nav link ask the same question and a unit test
  reaches it directly.
- **The menu asks `IsAuthorizedAsync`, the services ask for a grant held in the platform account.**
  They agree because the menu only appears while the platform account is the current account.
- **The platform Owner role keeps its old system defined `platform_settings` row,** which nothing
  checks. The migration guide says so.
- **Checked in the browser** with a throwaway page and a security only platform member, all removed
  afterwards: the menu, the redirect, the nav link and the refusal on the Audit page behave as
  planned, and switching to the host page happens without a reload.

## Problem Statement

Platform settings should follow the same rule as the rest of IAM: a platform owner builds thin
roles, and each person holds only what their job needs. Two things stop that today.

1. **IAM's own two areas share one permission.** The Audit and the Security settings pages both
   check `platform_settings`, so whoever may change the password rules may also switch auditing
   off.
2. **A host cannot add an area.** The portal under `/platform-settings` lists only Audit and
   Security, so a host's platform wide settings (DocsToData's SFTP configuration or allowed
   extraction providers, say) have no place beside them, and a library such as Corely.Billing could
   not add one either.

The menu also shows every tab to every platform account member, and the "Platform settings" link
always lands on Audit, which refuses anyone who holds only Security.

## The rule

**One settings area, one page, one resource type.** Reading the area needs Read on its type;
changing it needs Update. Never one type per field: an area holding something much riskier than the
rest of it should become its own area.

## What stays the host's

IAM does not store, shape or interpret host settings. A host already has everything else:

- **Storage:** its own tables, in its own shape.
- **Access:** its own resource type, registered with no owner actions, for example
  `options.RegisterResourceType("d2d_sftp_settings", "SFTP settings")`, and checked in its service's
  authorization decorator with `IAuthorizationProvider.IsAuthorizedAsync`. The platform owner
  receives every registered type at startup; an account owner cannot grant themselves one they do
  not hold.
- **Auditing:** its audit decorator records through `IAuditProvider`.
- **Picking up changes:** its own caching.

Per account features are out of scope: a host already provisions those per customer, as
Corely.Billing's grants do.

## Back end: `Corely.IAM`

1. **`platform_settings` splits in two.**
   - `PlatformSettings.RESOURCE_TYPE` becomes `"platform_audit_settings"`, checked by
     `IAuditService.GetPlatformSettingsAsync` and `UpdatePlatformSettingsAsync`.
   - `SecurityPolicy.RESOURCE_TYPE`, new, is `"platform_security_settings"`, checked by
     `IPlatformService.GetSecuritySettingsAsync` and `UpdateSecuritySettingsAsync`.
   - Both are registered as IAM types with no owner actions, as `platform_settings` is today, and
     the audit decorators record each call against its own type.
2. **Nothing else changes.** The startup service gives the platform owner both types with the rest.
   Existing grants of `platform_settings` are not migrated: the only ones outside a fresh bootstrap
   are in the DocsToData proof of concept's development database, edited by hand when it upgrades.

## Front end: `Corely.IAM.Web`

1. **A settings page is described once,** as a public record:

   ```csharp
   public record PlatformSettingsPage(string Title, string Path, string Icon, string ResourceType);
   ```

   IAM describes its own two this way. A host or a library adds its own through `IAMWebOptions`,
   following the `ForgotPasswordPath` pattern. Configuration stacks, so each layer adds its page
   without knowing about the others:

   ```csharp
   // an app
   builder.Services.AddIAMWeb(o =>
       o.PlatformSettingsPages.Add(new("SFTP", "/platform-settings/sftp", "bi-hdd-network", "d2d_sftp_settings")));

   // a library's own setup method
   services.Configure<IAMWebOptions>(o =>
       o.PlatformSettingsPages.Add(new("Billing", "/platform-settings/billing", "bi-receipt", "billing_settings")));
   ```

2. **`PlatformSettingsFrame` lists the pages the user can read:** IAM's two, then the configured
   ones in the order they were added, each shown only with Read on its type, asked of
   `IAuthorizationProvider` as `PermissionView` does.
3. **`/platform-settings` opens the first page the user can read,** instead of always Audit, and
   says so when there is none.
4. **The nav bar's "Platform settings" link goes to `/platform-settings`** and shows only when the
   user can read at least one page.

A host's page is an ordinary Razor component at its own route that wraps its content in
`<PlatformSettingsFrame>`, as IAM's pages do, so it shares the header and side menu and switching
between areas happens in place. DocsToData's admin portal already routes IAM's pages and its own
through one Blazor router with the same render mode, so its pages fit as they are.

## DocsToData, later, in its own repository

- Its nav bar links straight to IAM's Audit page; it should link to `/platform-settings`.
- Upgrading to Corely.IAM 5.0 changes `using Corely.IAM.Security.Constants` to
  `Corely.IAM.Security.Enums`.

## Testing

- **Unit:** each settings service decorator refuses without its own type and passes with it, and
  holding one type does not open the other. bUnit: the frame shows a page only with Read on its
  type, lists configured pages after IAM's, and the redirect picks the first readable page.
- **Integration:** a platform member holding only `platform_security_settings` reads and saves the
  security settings and is refused the audit settings.
- **In the browser:** in a local run of `Corely.IAM.WebApp`, register a throwaway host page (not
  committed), give a platform member a role holding only one area's type, and check the menu, the
  redirect and the nav link.

## Docs

`platform.md` (the settings section and the roles table), `auditing.md`, `resource-types.md`, and
`MIGRATION-5.0.md` for the split type.

## Release

Rides in the unreleased Corely.IAM 5.0.0 and Corely.IAM.Web 6.0.0.

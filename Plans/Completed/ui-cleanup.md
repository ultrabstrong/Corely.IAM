# UI cleanup in Corely.IAM.Web

**Status: released** in `Corely.IAM` 5.1.0 and `Corely.IAM.Web` 6.1.0, with fixes from the release
click through in 6.1.1 (sign in card position on a phone, audit table scroll, long demo notes) and
6.1.2 (contrast of faint text, the caution button, dark outline buttons and inactive action badges).

Eight fixes to `Corely.IAM.Web`'s pages, most of them found by clicking through DocsToData's portal
on a phone. Each stands alone; they share a plan because they share a release and a click through.

## 1. Link the accept invitation page into the account pages

The account pages a user moves between before they are in an account are joined by links, except
one:

| Page | Links to |
|---|---|
| Select account (`/select-account`, Razor page) | Create account |
| Create account (`/create-account`, Razor page) | Select account ("Back to accounts") |
| Accept invitation (`/accept-invitation`, Blazor page) | Nothing |

Nothing links to accept invitation either, apart from the WebApp's dashboard and a host's home
page. A user who opens it by mistake, or decides to create their own account instead, has only the
browser's back button.

1. `Components/Pages/Invitations/AcceptInvitation.razor`: a "Back to accounts" link under the card,
   to `AppRoutes.SelectAccount`, worded and placed like the one on `CreateAccount.cshtml`. A Razor
   page, so the link takes `data-enhance-nav="false"`.
2. `Pages/Authentication/SelectAccount.cshtml`: an "Accept an invitation" link under "Create New
   Account", to `AppRoutes.AcceptInvitation`, in the same `btn btn-outline-primary w-100` style with
   the `bi-envelope-open` icon.

Test: the functional tier renders `/select-account` for a signed in user and finds the link.

## 2. Inputs that stand out from the card behind them

Text boxes, text areas and selects use `--bs-body-bg`, and so do `.section-divider` cards, so in both
themes an empty input is only a thin border on the same colour. The page reads as one flat surface.

Give form controls their own background in `wwwroot/css/iam-web.css`: a token per theme
(`--iam-input-bg`), slightly darker than the card in light mode and slightly lighter in dark mode,
applied to `.form-control` and `.form-select`, with the disabled and read only states kept visibly
different from both. Hosts that load `iam-web.css` get it too, which is the point: DocsToData's portal
shows the same flatness.

Check every input state in both themes: empty, filled, focused, invalid, disabled, read only.

## 3. Every table becomes cards on a phone

`table-cards` turns rows into cards below 768px, and is on every table in the package except
`Components/Shared/AuditActionMatrix.razor`, the audit settings matrix on the account and platform
audit pages. On a phone that matrix scrolls sideways.

1. **The matrix.** One card per scope row: the scope as the card's title, then the action checkboxes
   in a row beneath it, each labelled with the action's name rather than its letter (the letters only
   make sense under the column headers, which the cards hide).
2. **The rule.** Add to `CLAUDE.md`, under a UI section: every `<table>` carries `table-cards`, with
   its cells given the `cell-*` roles, so it reads as cards on a phone. A table that cannot is a
   design question, raised before it is built.

No test enforces it: one would have to read source files from disk, which ties it to the folder
layout, for a rule that about fifteen tables already follow. The rule and the phone click through
before release cover it.

## 4. Encryption and signing on a phone

`Components/Shared/EncryptionSigningPanel.razor` (profile, user and account pages) is cramped below
768px: three `flex-nowrap` tabs squeeze together, and each tab's row of up to four buttons (Encrypt,
Decrypt, Re-encrypt, Rotate key) shares one line.

1. **Tabs:** let them scroll sideways inside the card instead of squeezing (`overflow-x: auto` on the
   tab list, no wrap), or show icon and short label only. Pick whichever looks right on a 390px
   screen; both keep the labels readable.
2. **Buttons:** wrap onto two lines on a phone, each at least half the card's width, with Rotate key
   on its own line so a destructive action is not beside an everyday one.

Check at 390px and 768px, both themes, every tab.

## 5. Confirm platform setting changes before saving them

`PlatformAuditSettingsPage.razor` and `PlatformSecuritySettingsPage.razor` save the moment Save is
pressed. These settings apply to every account, and some changes weaken the whole platform:
turning auditing off (`AuditEnabled`) or unticking audited actions such as reads, shortening
`AuditMaxRetentionDays`, lowering the password minimum length or dropping a character rule, raising
`MaxLoginAttempts`, shortening `LockoutCooldownSeconds`, lengthening token and session lifetimes.

Save opens `ConfirmModal` (already in `Components/Shared`) listing each changed setting as old value
to new value. A change that weakens security or auditing is listed first and marked as such, and
makes the modal use its warning style. Nothing saves until the modal is confirmed; cancelling leaves
the edits on the page.

Which changes count as weakening is a reading of the setting, so it gets a seam of its own: a member
on `SecurityPolicy` and on `PlatformSettings` that compares two values and returns the weakening
changes, with unit tests per setting. The audited actions matrix is already covered by the
`PlatformSettings` member: its three rows are the `AuditActions` flags `AuditAllowedActions`,
`SystemContextActions` and `AccountlessActions`, and dropping a flag from any of them weakens auditing.
Which flags were dropped is a reading of the enum, so it goes on the existing
`Audits/Models/Extensions/AuditActionsExtensions.cs`, tested there.

## 6. Section titles in a card header, without the line

`.section-divider-label::after` in `iam-web.css` draws a 2px rule from every section title to the
right edge of its card ("Users", "Details", "Invitations" on the account page, and every detail page).
Inside a card that already has a border it is only clutter, and the title sits in the card's body
with nothing setting it apart.

Give each section the shape DocsToData's portal uses on its workflow page
(`DocumentWorkflowEditor.razor`, "Workflow Details" and "Steps"): a Bootstrap `card` whose
`card-header` holds the title on the left and the section's buttons on the right
(`d-flex justify-content-between align-items-center`), on the header's own tinted background, with
the content in `card-body`.

1. Change `.section-divider` and `.section-divider-label` to that shape, or replace them with `card`,
   `card-header` and `card-body` in the markup if restyling the existing classes cannot reach it
   cleanly. Either way the `::after` rule and its `order` trick go.
2. Keep the header's title style (weight, size, colour) consistent with `.detail-section
   .card-header`, which already has this shape for some panels, so there is one section header look
   in the package, not two.
3. A table directly in a section sits flush to the card's edges under the header, as it does on the
   portal's steps card.

Check every page that uses `.section-divider`: account, user, group, role and permission details,
profile, audit entry, both platform settings pages, and the shared sections (password, TOTP, linked
accounts, encryption and signing, effective permissions, audit settings).

## 7. Say when an account name is taken

`CreateAccount.cshtml.cs` shows "Failed to create account. Please try again." for every failure. A
name already in use is the common one, and the user cannot fix it from that message.

The reason is lost in `Corely.IAM` before the page sees it: `AccountProcessor` returns
`CreateAccountResultCode.AccountExistsError`, but `RegistrationService` turns every account creation
failure into `RegisterAccountResultCode.AccountCreationError`.

1. Add `AccountExistsError` to `RegisterAccountResultCode` and map it from the processor's code.
   Update `Docs/result-codes.md`.
2. `CreateAccount.cshtml.cs`: "An account named {name} already exists. Choose another name." for
   that code; the generic message stays for the others.
3. `Corely.IAM.WebApp/Components/Pages/Home.razor` and
   `Corely.IAM.Demos.SharedAccount/Components/Pages/Home.razor` create accounts too and show
   `result.Message`; give them the same wording.

Tests: the registration service maps the code (unit, in `RegistrationServiceTests`); the page shows
the message (functional). Check `AuditDecoratorMappingTests` for a mapping of
`RegisterAccountResultCode` that needs the new value.

## 8. The sign in layout's user menu on a phone

`Pages/Shared/_AuthLayout.cshtml` puts the signed in user's menu in a header marked
`navbar-expand-lg` with no collapse button. Below 992px Bootstrap lays a navbar's menus out inline,
as it would inside a collapsed hamburger, so opening the menu pushes the header taller instead of
dropping over the page. The header has no hamburger, so it should be `navbar-expand`, expanded at
every width. DocsToData's portal hit this in its copy of the layout and fixed it the same way.

`Pages/Shared/_LegalLayout.cshtml` has the same header with no menu in it, so nothing breaks there
today; change it to `navbar-expand` too so the two headers stay alike. Check the demo hosts' layouts
for the same header.

## Release

`Corely.IAM` gains an enum value (7), a minor version. `Corely.IAM.Web` gets markup and styles
(1 to 8), a minor version that depends on the new `Corely.IAM`, so `Corely.IAM` is released
first. A host that switches exhaustively over `RegisterAccountResultCode` gets a new case to handle;
say so in the release notes.

Click through the WebApp and both demo hosts at 390px, 768px and desktop, light and dark, before
release.

## As built

- **3.** No test enforces the rule; the plan dropped it before building. The rule is the "UI" section
  of `CLAUDE.md`.
- **5.** The changes are `SettingChange` records (a public `PlatformSetting` enum names the
  setting) from `SecurityPolicy.ChangesTo` and `PlatformSettings.ChangesTo`, weakening changes
  first; dropped audit flags come from `AuditActions.Dropped`. The modal lists each change as a
  sentence saying what it will do ("Accounts can no longer record Read. Accounts that record Read
  now will stop."), from `SettingChangeExtensions.Consequence()` in `Corely.IAM.Web`, through a new
  optional `ChildContent` on `ConfirmModal`. A save with no changes skips the modal. Code prompt timeout, recovery
  codes per user and maximum password length never count as weakening.
- **6.** Done in `iam-web.css` alone: `.section-divider-label` became the tinted header, no page
  markup changed. While editing, a section's Save and Cancel replace its Edit button in that header
  rather than sitting under the body (audit settings, both platform settings pages, and the account,
  role, group and permission details).
- **Added during the click through.**
  - Below 576px, cards and sections run the full screen width with no side border or radius. Every
    layout's main element carries `iam-main`, and the rule targets `.section-divider`, `.auth-card`
    and `.iam-card` inside it.
  - The light mode table header is lighter (`--iam-thead-bg`).
  - A system defined role or permission is marked one way everywhere: the SYSTEM badge under its
    name and GUID, never a "Managed by" column or the Actions column.

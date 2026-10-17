# UI cleanup in Corely.IAM.Web

**Status: new.** Designed, not started.

Seven fixes to `Corely.IAM.Web`'s pages, most of them found by clicking through DocsToData's portal
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

## 3. Every table becomes cards on a phone, enforced by a test

`table-cards` turns rows into cards below 768px, and is on every table in the package except
`Components/Shared/AuditActionMatrix.razor`, the audit settings matrix on the account and platform
audit pages. On a phone that matrix scrolls sideways.

1. **The matrix.** One card per scope row: the scope as the card's title, then the action checkboxes
   in a row beneath it, each labelled with the action's name rather than its letter (the letters only
   make sense under the column headers, which the cards hide).
2. **The rule.** Add to `CLAUDE.md`, under a UI section: every `<table>` carries `table-cards`, with
   its cells given the `cell-*` roles, so it reads as cards on a phone. A table that cannot is a
   design question, raised before it is built.
3. **The test.** A unit test in `Corely.IAM.Web.UnitTests` reads every `.razor` and `.cshtml` file in
   the package and fails for a `<table` without `table-cards`, naming the file. An exception goes in
   the test's allowed list with the reason beside it, the way `PublicSurfaceTests` keeps its list. It
   fails today on the matrix, which proves it.

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
changes, with unit tests per setting. The audited actions matrix needs the same reading; find where
its selections are modelled and put the member there.

## 6. No line after section titles

`.section-divider-label::after` in `iam-web.css` draws a 2px rule from every section title to the
right edge of its card ("Users", "Details", "Invitations" on the account page, and every detail page).
Inside a card that already has a border it is only clutter.

Remove the `::after` rule and its `order` trick, and keep the title's buttons on the right with
`justify-content: space-between`. Check every page that uses `.section-divider`: account, user, group,
role and permission details, profile, audit entry, both platform settings pages.

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
3. `Corely.IAM.WebApp/Components/Pages/Home.razor` creates accounts too and shows `result.Message`;
   give it the same wording.

Tests: the registration service maps the code (unit); the page shows the message (functional).

## Release

`Corely.IAM` gains an enum value (7), a minor version. `Corely.IAM.Web` gets markup, styles and a
test (1 to 7), a minor version that depends on the new `Corely.IAM`, so `Corely.IAM` is released
first. A host that switches exhaustively over `RegisterAccountResultCode` gets a new case to handle;
say so in the release notes.

Click through the WebApp and both demo hosts at 390px, 768px and desktop, light and dark, before
release.

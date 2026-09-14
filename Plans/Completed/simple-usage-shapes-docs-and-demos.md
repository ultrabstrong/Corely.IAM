# Simple usage shapes: a docs page and two demo apps

## The problem

Corely.IAM reads as a full RBAC system - accounts, groups, roles, permissions - and every example
in the repository uses all of it. It also works for much simpler apps, but nothing says so and
nothing shows how:

1. **Users only.** Someone signs up with a username and password and is running. No accounts, no
   roles. The user owns their own data outright.
2. **One account, many users.** A team shares an account. The owner invites people; nobody manages
   groups, roles, or permissions.

Both work today with no library change. This plan is documentation and demonstration only.

## What the code already supports

Verified by reading, not assumed:

- `RegisterUserAsync` creates a user and a password and nothing else. `SignInAsync` takes an
  optional `AccountId`; a user with no accounts signs in and gets a context with
  `CurrentAccount == null`.
- `PostAuthenticationFlowService` sends a user with no accounts straight to `/`, and a user with
  exactly one account is switched into it automatically. So neither shape ever shows the account
  picker.
- Creating an account makes the creator its owner, with the default roles and wildcard permissions.
  Invitations are created by someone with rights on the account - the owner - and accepting one
  makes the invitee a member.
- A member with no roles can sign in to the account - switching checks membership - but every IAM
  operation on it is a CRUDX check: reading the account, listing users, creating invitations. Those
  are owner-only until someone assigns roles, which suits the team shape: the owner invites, members
  use. (An earlier draft said account operations authorize by membership. Only list *scoping* does.)

### Where it does not help, and why that is fine

- **Users only gets authentication, not authorization.** Permissions live inside accounts. With no
  account, the app scopes its own data by `UserContext.User.Id`. A single user has every right to
  their own data, so there is nothing to administer.
- **Default roles for invitees are not being added.** An app that wants "every invitee is an admin"
  assigns the role itself after acceptance. Pre-assignment stubs on invitations were considered and
  rejected: users are configured once they exist, and ergonomics get added when someone asks.

## Findings that shape the demos

- **The Blazor pages come as a set.** `AddAdditionalAssemblies(typeof(AppRoutes).Assembly)` routes
  `/profile` *and* `/users`, `/groups`, `/roles`, `/permissions`. A simple app that wants the profile
  page gets the admin portal with it. The profile is built from public components - `TotpSection`,
  `PasswordSection`, `LinkedAccountsSection` - so a host composes its own page instead and never adds
  the assembly. The demos do that. A library change is not proposed until a consumer asks.
- **The auth pages are branded "Corely IAM".** `_AuthLayout.cshtml` hard-codes the name in the
  navbar, title, and footer, and links to `/create-account`. Razor Pages lets an app file override an
  RCL file at the same path, so a host supplies its own `Pages/Shared/_AuthLayout.cshtml`. The demos
  do that too. The override must keep loading `form-busy.js`, or the busy state is lost silently.
- **The account pages stay reachable.** `/create-account` and `/select-account` are Razor Pages and
  are mapped by `MapRazorPages()` whatever the host does. In the users-only shape a user who finds
  `/create-account` gets an account the app ignores. Harmless - their data is keyed by user, not
  account - and documented rather than blocked.

## Deliverables

### 1. Docs page - `Corely.IAM/Docs/usage-shapes.md`

House style: a short orienting paragraph, then code. Covers:

- The three shapes (users only, one shared account, full RBAC) and which parts of the library each
  uses.
- Users only: register, sign in, scope app data by `UserContext.User.Id`.
- Shared account: create the account on first sign-in (or accept an invitation), invite members,
  scope app data by `UserContext.CurrentAccount.Id`.
- The unused tables stay in the schema. That is expected; the migrations are not split per shape.

Linked from `Corely.IAM/Docs/index.md`. A short section in `Corely.IAM.Web/Docs/setup.md` covers the
Web-side findings above: skip the Blazor assembly, compose the profile, override `_AuthLayout`.

### 2. Demo: users only - `Corely.IAM.Demos.UsersOnly`

A personal notes app. Blazor Server host.

- Sign in and register come from `Corely.IAM.Web`'s Razor Pages, under the demo's own `_AuthLayout`.
- `/` lists the signed-in user's notes and adds or deletes them. Notes live in the demo's own
  `DbContext`, keyed by user id.
- `/profile` is the demo's own page, composed from the public profile components.
- Does not add the Web Blazor assembly to the router, so no admin pages exist.

### 3. Demo: shared account - `Corely.IAM.Demos.SharedAccount`

A team notes app. Same host shape as the first demo.

- First sign-in with no account shows two choices: create a team, or paste an invitation token.
- `/` lists the team's notes, keyed by account id. Every member sees and edits them.
- `/team` lists members. The owner creates invitations there and sees the token to hand over; the
  demo shows it in the page rather than sending email, the same way the main WebApp previews
  password recovery.
- Accepting an invitation, then switching into that account, goes through `/switch-account`.

### Shared decisions

- **SQLite with `EnsureCreated`, so each demo runs with `dotnet run` and nothing else.** Production
  schema comes from the `corely-iam-db` tool and that stays the only documented path; the demos say
  so in their README and at the top of `Program.cs`. IAM and the demo's notes use **separate
  database files**: `EnsureCreated` does nothing when a database already has tables, so a second
  context on the same file would never get its schema.
- **A committed development-only system key** in `appsettings.Development.json`, labelled as such.
  It protects demo data in a local file and nothing else.
- **Projects sit at the repository root** beside `Corely.IAM.WebApp`, and are added to the solution
  so CI builds them.
- **One functional smoke test per demo**: register through the real pages, land on `/`, create a
  note, see it. Enough to catch a demo that stops starting or stops working without anyone opening
  it. `WebApplicationFactory` is keyed on a type from each demo's assembly, since every host's
  `Program` is the same global name.

## Out of scope

- Any change to `Corely.IAM` or `Corely.IAM.Web`. No version bumps.
- Default-role assignment, invitation pre-assignment, or a profile-only Blazor assembly.
- Email delivery of invitations.

## Status

Done. Docs: `Corely.IAM/Docs/usage-shapes.md` and a Simple Apps section in `Corely.IAM.Web/Docs/setup.md`.
Demos: `Corely.IAM.Demos.UsersOnly` and `Corely.IAM.Demos.SharedAccount`, both walked through in a
browser - sign-up, notes, profile with the MFA QR code, team creation, invitation, joining as a
second user, a member refused the member list, and a one-team user landing in the team on sign-in.

Where the build departed from the plan:

- **LocalDB and the `corely-iam-db` tool, not SQLite with `EnsureCreated`.** `IamDbContext` is
  internal, so a host cannot create IAM's schema itself - which is the intended design, and the demos
  now follow the same path a real deployment does. Each demo's own notes live in a second database
  that the demo creates on startup, since `EnsureCreated` does nothing to a database that already
  has tables.
- **A third project, `Corely.IAM.Demos.Assets`.** Linking the WebApp's vendored Bootstrap into the
  demos fails at startup - static web assets resolve linked files against a `wwwroot` that does not
  exist. A small Razor class library serves one copy to both.
- **The smoke tests live in `Corely.IAM.Web.FunctionalTests/Demos`**, which already has access to
  IAM's internals for creating the schema on SQLite. .NET 10 makes a top-level `Program` public, so
  the three hosts collide by name; the demos are referenced through extern aliases. The tests prove
  sign-up lands on `/`, that the admin pages return 404, and that the sign-in pages carry the app's
  layout and the busy-state script. Routing the admin pages in a demo was confirmed to fail them.
  What happens after sign-in is Blazor over a circuit and out of this tier's reach.

Found along the way, not fixed here:

- **Google sign-in is blocked by this package's own Content-Security-Policy.**
  `SecurityHeadersMiddleware` sets `script-src 'self' 'unsafe-inline'`, and the sign-in, register,
  and link-account pages load `https://accounts.google.com/gsi/client`. With a client id configured,
  the browser reports `script-src-elem blocked https://accounts.google.com/gsi/client`. Google's
  button renders in an iframe, which `default-src 'self'` would block next - not yet observed,
  since the script never loads.
- **Sign-up validation errors say only "Validation for User failed".** A username under the minimum
  length gets that message with no hint of which rule failed.
- `Corely.IAM/Docs/index.md`'s quick start built `RegisterAccountRequest` without the owner id, and
  the WebApp README still described the deleted `config init` command. Both fixed.

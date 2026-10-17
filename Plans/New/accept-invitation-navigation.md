# Link the accept invitation page into the account pages

**Status: new.** Designed, not started.

## What

The account pages a user moves between before they are in an account are joined by links, except
one:

| Page | Links to |
|---|---|
| Select account (`/select-account`, Razor page) | Create account |
| Create account (`/create-account`, Razor page) | Select account ("Back to accounts") |
| Accept invitation (`/accept-invitation`, Blazor page) | Nothing |

Nothing links to accept invitation either, apart from the WebApp's own dashboard and a host's home
page. A user who opens it by mistake, or who arrives from an invitation email and decides to create
their own account instead, has only the browser's back button. DocsToData's portal found this from
its new empty state home page, which links here.

## Change

1. `Components/Pages/Invitations/AcceptInvitation.razor`: a "Back to accounts" link under the card,
   to `AppRoutes.SelectAccount`, worded and placed like the one on `CreateAccount.cshtml`. A Razor
   page, so the link takes `data-enhance-nav="false"`, as the nav bar's links to Razor pages do.
2. `Pages/Authentication/SelectAccount.cshtml`: an "Accept an invitation" link beside "Create New
   Account", to `AppRoutes.AcceptInvitation`, so a user choosing an account can also join one. Same
   `btn btn-outline-primary w-100` style, `bi-envelope-open` icon, the two stacked with a small gap.

Select account then reaches both other pages, and both come back to it.

## Tests

1. Functional tier, `Corely.IAM.Web.FunctionalTests`: `/select-account` for a signed in user
   renders a link to `/accept-invitation`. The tier already signs users in through the real
   registration service.
2. The accept invitation page is a Blazor component, which the functional tier only reaches as the
   initial shell. Check whether `Corely.IAM.Web.UnitTests` renders components; if it does, assert the
   link there. If not, check it by hand in the WebApp: the E2E tier is deliberately empty, and a link
   is not a reason to start it.

Prove 1 catches the regression: remove the link, watch it go red, restore.

## Release

Additive markup in `Corely.IAM.Web`: a minor version bump. `Corely.IAM` is unchanged. Hosts pick it up
with no change of their own.

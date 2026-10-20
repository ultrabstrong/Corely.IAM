# Auth cookies that survive a link from another site

**Status: released** as Corely.IAM.Web 7.0.1; DocsToData moves to it in the same change.

## Why

DocsToData's grant request email links to `/accounts/<id>/grants`. Opened from the Gmail app while
already signed in, the portal showed the sign in page, and signing in from there failed with a 400.

The cause is `SameSite=Strict` on the two auth cookies (`authentication_token`, `auth_token_id`).
A browser withholds a Strict cookie on any request a navigation from another site started, a
clicked link included, so the request arrives signed out. The sign in form is then built for an
anonymous visitor, but posting it is same site, the cookies come back, and the antiforgery token no
longer matches the user: 400. DocsToData's `CrossSiteLinkTests` reproduces the first half: signed in,
a link clicked on another site lands on `/signin`.

## Best practice

- OWASP's CSRF cheat sheet: Strict blocks the cookie "even on ordinary links"; Lax is "a reasonable
  balance between security and usability", on condition that no GET changes server state and that
  SameSite stays defence in depth beside CSRF tokens. It also lists Fetch Metadata
  (`Sec-Fetch-Site`) as a defence.
- MDN: Lax sends the cookie on top level navigations with a safe method, never on another site's
  POST or on subresource requests.
- ASP.NET Core Identity's own auth cookie is Lax.

Blazor Server's circuit opens over a WebSocket, a subresource request, so Lax never sends the cookie
on one another site starts.

## Design

1. **Auth cookies are `SameSite=Lax`.** The device id cookie stays Strict: it is only read on same
   site posts.
2. **No GET changes state when another site started it.** Two IAM GETs change state today:
   `/signout` (revokes the session) and `/switch-account` (issues a token for another account). Each
   acts on GET only when `Sec-Fetch-Site` is `same-origin`, which is how a host's own links and
   `AccountScope` reach them. Otherwise, the header absent included, it renders a confirm button that
   posts with an antiforgery token; the POST does the work. A self submitting page would be no
   defence, since another site could open it.
3. **The rule is tested.** A functional test sends a cross site GET to every endpoint in `AppRoutes`
   with a signed in client and asserts no auth cookie is set or deleted and the session still works.
4. `CLAUDE.md` gains the rule; `Docs/authentication-flow.md` and `Docs/security.md` say Lax and why.

Considered and left out: GitHub's second Strict cookie required on every state change. It guards
against a state changing GET slipping in later, which the test in 3 already catches.

## Release

Corely.IAM.Web 7.0.1. Then DocsToData moves to it, keeps `CrossSiteLinkTests` as the proof, and is
pushed.

## As built

1. **Same origin is read from `Sec-Fetch-Site`** through `HttpRequest.IsSameOrigin()`. Only
   `same-origin` counts: `same-site` (a sibling subdomain), `none` (typed or bookmarked) and a
   missing header all get the confirm button. A typed `/signout` therefore asks first.
2. **The confirm pages:** `/signout` says "Sign out?" with one button; `/switch-account` says the link
   opens a different account and carries `accountId` and the local `returnUrl` into its form.
3. **Tests.** `CrossSiteRequestTests` covers every `AppRoutes` endpoint with `cross-site`,
   `same-site` and no header (37 cases): ten failed before the change, the sign out and switch GETs
   and the four confirm cases. `SignInTests` asserts Lax on both auth cookies. The existing sign out
   and switch tests now send `same-origin`, as a browser does from the host's own links.
4. **Proof in DocsToData:** `CrossSiteLinkTests` follows a Grants link from a page on another site,
   once into the current account and once into another; both landed on `/signin` on 7.0.0 and on
   the Grants page on a local 7.0.1.
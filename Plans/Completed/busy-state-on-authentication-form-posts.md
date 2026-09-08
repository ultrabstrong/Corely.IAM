# Busy state on the authentication form posts

## The problem

Signing in takes several seconds against a deployed environment and the page gives no feedback at
all. Nothing disables, nothing spins, the button stays live and the form looks like it ignored the
click.

The reassurance is the smaller half. **The submit button stays clickable for the whole request**, and
a second click posts the credentials again - issuing a second auth token, a second set of database
round-trips, and a race between two sign-ins for the same user.

An earlier draft of this plan claimed a double submit could lock the account out via
`SecurityOptions:MaxLoginAttempts`. It cannot: `AuthenticationService` increments
`FailedLoginsSinceLastSuccess` only when password verification *fails*, so resubmitting correct
credentials counts nothing. This is a UX fix, not a correctness one - worth doing, but not urgent
on a risk that does not exist.

Applies equally to the other posts in this package — register and password recovery are the same
shape and the same risk of a duplicate submission.

## Recommended treatment

**Disable the submit button and swap its label for a spinner.** Not an overlay.

- The button is where the user's attention already is, and disabling it is what actually prevents the
  double post. The spinner exists to make the disabled state read as "working" rather than "broken".
- Progressive enhancement: a submit handler, no framework. With scripting unavailable the form still
  posts, which is the property an overlay would not have.
- **Delay ~150 ms before showing the spinner.** A sign-in that completes in 80 ms should show
  nothing; a spinner that flashes reads as a glitch.
- **Fix the button's width** so swapping the label for a spinner does not shift the card.
- Set `aria-busy` and the disabled attribute together, so the state is not merely visual.

### Why not an overlay modal

Recorded because it is the obvious alternative:

- It blocks a whole page for something the user initiated at one control.
- It needs focus management and `aria-modal` to be accessible — more to build and more to get wrong.
- The response *replaces the page*, so on any fast sign-in the overlay appears and vanishes as a
  flash.
- It does not do the one thing that matters here — preventing the second post — any better than
  disabling the control does.

## Scope

One shared behaviour in `Corely.IAM.Web`, applied to the authentication pages rather than written per
page. These are Razor Pages posts, so there is no component to bind to and no circuit; whatever is
built is a small script plus the markup hook it looks for.

Deliberately **not** a shared Blazor `BusyButton` component. Consuming apps have that need too — the
DocsToData portal has fifteen hand-written spinners across eleven components — but a busy button is
not an identity concern, and this package exporting general UI controls because it happens to ship an
RCL is how an auth library ends up owning date pickers. That consumer is building its own; the
overlap is coincidental.

## Worth pairing with

Why sign-in takes seconds at all is not established. It does real work — `SignInAsync` → auth token
creation → `DecryptWithSystemKey`, plus database round-trips — but nobody has measured which part
dominates. The consumer that reported this has no request or dependency telemetry in its deployed
environment, so it cannot answer the question from its side either. If the answer turns out to be
something this library controls, the indicator matters less than the fix.

Note for anyone grepping: the timing logs are written with `LogTrace`, not `LogVerbose` - Verbose is
the Serilog name for the same level.

## Status

Implemented in `Corely.IAM.Web` 2.1.0.

`wwwroot/js/form-busy.js`, loaded by `_AuthLayout` so every authentication page gets it and neither
a page nor a consuming host opts in. On submit of a `method="post"` form it disables the submit
buttons and sets `aria-busy`; buttons marked `data-busy-spinner` also swap their label for a spinner
after 150 ms, with the width pinned first.

Two things the plan did not anticipate, both found by reading the pages rather than assuming:

- **Not every submit button should become a spinner.** Select Account renders one form per account
  whose button *is* the account name, and its search form is a `GET`. A blanket rule would have put
  a spinner where the label is content, and on a search. The spinner is opt-in per button; the
  disable applies to every post.
- **Password recovery is not in this package.** The plan listed it alongside sign-in and register,
  but `ForgotPasswordPath` is supplied by the host - those pages belong to the consumer.

Disabling is deferred by a tick so the button's own name and value still reach the server; a
disabled control is omitted from form data.

Verified two ways. Functional tests hold the contract the script depends on - that it is served and
that the markup carries the hook. Behaviour was exercised against a real DOM under jsdom, covering
the disable, the aria-busy, the delayed swap, the pinned width, and both exclusions: a GET form left
untouched and a list button disabled without losing its label.

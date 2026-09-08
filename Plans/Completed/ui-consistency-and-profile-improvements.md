# UI consistency and profile page improvements

A wishlist from using the deployed portal. Grouped by what each item actually is, because they are
not the same kind of work: two are defects with identified root causes, one is a missing feature,
and the rest are presentation.

Items marked **consumer** also apply to DocsToData and cannot be finished here alone.

## Defects

### 1. Enabling TOTP fails

**Root cause found.** `TotpSection` renders its QR code by calling `qrCodeInterop.generate`, a
function defined in two scripts the *host* must load:

```html
<script src="_content/Corely.IAM.Web/lib/qrcodejs/qrcode.min.js"></script>
<script src="_content/Corely.IAM.Web/js/qrcode-interop.js"></script>
```

`Corely.IAM.WebApp/Components/App.razor` loads both. The DocsToData admin portal loads
`iam-web.css` and neither script, so the call hits an undefined function and throws.

Two things make it worse than a missing script:

- **The throw is in `OnAfterRenderAsync`, outside `RunSafeAsync`'s try/catch.** It is not the handled
  "An unexpected error occurred" path; it is an unhandled render exception.
- **Enabling already succeeded server-side before the QR render is attempted.** `EnableTotpAsync`
  returns, the secret is stored, and only then does rendering fail - so the user is left with a
  pending enrollment they cannot see a code for and cannot complete.

The fix is not to document the scripts. A component that silently requires host wiring will keep
failing for the next consumer, and this is the same shape as the migration CLI gap: a dependency the
package knows about but the consumer has no way to discover.

**Make `TotpSection` load its own dependency.** Blazor JS isolation (`IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`)
lets the component import the module itself, so no host has to know. Keep the existing script tags
working for hosts that already have them.

Whatever the mechanism, the QR failing must not strand the enrollment: show the secret as text so
the user can enter it manually, since that is a supported way to add an authenticator and the secret
is already on screen in that phase.

### 2. Empty state flickers before the data is known — **consumer**

`EntityPageBase` declares `protected bool _loading;`, which defaults to `false`. The list pages
guard on:

```razor
@if (_loading && _items == null) { <LoadingSpinner /> }
```

On the first render pass `_loading` is still `false` and `_items` is still `null`, so it falls
through to the table and renders "no items yet". The spinner only appears once
`OnInitializedAsync` has set the flag.

This is the same defect as the `PermissionView` bug fixed earlier: an unknown state rendered as a
known one. The rule is the same - **only a confirmed empty result may render the empty state.**

Fix by making absence of data the loading condition rather than a flag that starts wrong:

```razor
@if (_items == null) { <LoadingSpinner /> }
else if (_items.Count == 0) { empty state }
else { table }
```

Setting `_loading = true` as its initial value would also work, but leaves two sources of truth for
one question. Prefer the shape that cannot drift.

Applies to every list page in this package, and to DocsToData, which reported it.

## Missing capability

### 3. Encryption and signing keys cannot be rotated — **dropped**

Not being built. Recorded because the capability really is absent, so the question will come back.

Note for whoever picks it up: the security layer is not the blocker. `Corely.Security` has versioned
key stores and `ReEncrypt`; what is missing is `Corely.IAM` exposing rotation on its services. The
retention question below is still the one that decides the size of the work.

<details>
<summary>Original scoping</summary>

There is no rotation API for user or account keys - no `Rotate`/`Regenerate` on
`IModificationService` or the processors, confirmed by search. `Corely.Security` supports key
versioning and `ReEncrypt`, so the primitives exist; IAM does not expose them.

Scope for a first pass:

- Regenerate a user's symmetric, asymmetric and signing keys
- Regenerate an account's, same three
- **Single active key.** Multiple concurrent versions are explicitly not needed.

Decisions to make before building, because they determine whether this is a small change or a
large one:

- **What happens to data encrypted with the old key?** The key store is versioned, so old values
  stay decryptable if the previous key is retained. "Single active key" must not be read as
  "discard the old one" - that destroys access to anything already encrypted.
- **Which permission gates it?** Rotating an account key is not the same authority as rotating your
  own.
- Whether rotation is exposed in the UI at all in the first pass, or only through the service.

</details>

## Presentation

### 4. Page header does not match the consumer's — **consumer**

IAM pages use a `Dashboard` text button and a `1.5rem` heading:

```razor
<div class="management-header">
    <h1><i class="bi bi-shield me-2"></i>Roles</h1>
    <a href="@AppRoutes.Dashboard" class="btn btn-outline-secondary btn-sm">Dashboard</a>
```

Two changes:

- **A back arrow**, matching the usage page, rather than a text-only button.
- **Larger heading and control text.** The `btn-sm` and `1.5rem` heading read smaller than the
  consumer's equivalent page.

Both live in `management-header` and the pages that use it, so this is one change applied in one
place. The target sizes should be taken from the consumer's usage page rather than guessed, so that
the two stop drifting.

### 5. Table rows should be vertically centred — **consumer**

Bootstrap top-aligns cells by default. `iam-web.css` sets `vertical-align: middle` only on
`.table-actions` and two other specific selectors, so the action column is centred while the
content columns beside it are not - which is why rows look misaligned rather than uniformly
top-aligned.

Centre by default at the table level and keep top alignment as the deliberate exception. Same
change in DocsToData.

### 6. Password section wording

Current:

> Add another sign-in method before removing your password.

It reads as encouragement to delete the password, which is not the intent - it is a precondition on
an action the user has not asked for. **Show the removal option only when another sign-in method
exists**, and drop the sentence. The state itself communicates the rule.

### 7. Linked accounts should be a list with actions

Current text when nothing is linked:

> No Google account linked. Use the Google Sign-In flow to link an account.

It names a flow without saying where it is, so it is not actionable.

Make it a static list of supported providers - today only Google - each row showing linked or not:

- **Linked:** the account address and an *Unlink* button. This already exists.
- **Not linked:** a *Link* button that starts the Google flow from this page.

Sign-in with Google is unchanged; this only adds linking from the profile.

### 8. Encryption and signing panel has no explanation

`EncryptionSigningPanel` presents providers and operations with no statement of what they are for.
Add a short description: the user gets symmetric and asymmetric encryption plus signing, usable for
encrypting and signing messages, and the panel is a place to exercise them.

The same panel appears on the account page and should carry the same description.

## Sequencing

1. **(1) and (2) first.** They are defects, and (1) leaves users with a broken enrollment.
2. **(4), (5), (6), (7), (8)** are independent presentation changes.
3. **(3) last** - it is the only item needing design decisions, and the key-retention question above
   should be settled before any code.

## Note on scope

(2), (4) and (5) are consumer-visible in DocsToData too. Fixing them here does not fix them there:
DocsToData has its own pages and its own tables. A companion plan in that repository should
reference this one rather than restating it.

## Status

Everything except key rotation is implemented in `Corely.IAM.Web` 2.2.0. Key rotation was dropped by
decision. The consumer-side halves of (2), (4) and (5) remain for DocsToData.

**(1) TOTP.** `TotpSection` now imports `js/totp-qrcode.js` itself, which loads the QR library on
demand, so no host has to add a script tag. A host that already loads them is unaffected - the
module skips loading when `window.QRCode` is present. Rendering failures no longer escape
`OnAfterRenderAsync`; they set a message pointing at the secret, which was already on screen and is
a supported way to enrol.

**(2) Empty state.** Absence of data is now the loading condition rather than a flag that starts
`false`. A load failure sets `_loadFailed` so a page that never got data stops spinning instead of
waiting forever - the first attempt at this fix traded a flicker for a hang, and the tests caught
it.

**(4) Header.** Back arrow on the dashboard link, heading to 1.75rem, and `btn-sm` dropped from
header controls only. Table-row action buttons stay small.

**(5) Tables.** `vertical-align: middle` applied at the table level, with `.align-top-cell` to opt
out.

**(6) Password.** Sentence removed. The remove option was already correctly gated on another
sign-in method existing, so the state communicates the rule on its own.

**(7) Linked accounts.** Now a provider list showing linked or not. `LinkGoogleAuthAsync` already
existed; what was missing was a way to reach it, so linking now happens on the page through Google's
own button, loaded on demand by `js/google-link.js`.

**(8) Encryption panel.** Description added, parameterised so the account page says "account" rather
than "user".

### On the tests

Four bUnit tests cover the empty state. Two things worth recording about getting there:

- **The first version of the test passed against the unfixed code**, because a synchronously
  completing mock never produces the interim render the bug lives in. The real reproduction holds
  the user-context task open, which is the same await that caused the `PermissionView` bug.
- **`mv` preserved an old timestamp and MSBuild skipped the rebuild**, so an earlier "does this test
  catch the bug" check was reading stale output. Re-run after touching the file, the test fails
  against the old markup and passes against the new.

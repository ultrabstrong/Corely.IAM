# Human verification on sign up and password recovery

**Status: done.** Released in Corely.IAM.Web 6.0.0.

## As built

Where the build differs from the plan below:

- **The refusal wording is on the result:** `VerifyHumanResult.RefusalMessage`, with the two
  messages as constants, so IAM's sign up page and a host's own page say the same thing.
  `Succeeded` reads the code. The types live in `Corely.IAM.Web.Security`.
- **`data-busy-spinner` needs nothing:** it disables the button only after the form has submitted,
  so a widget's token is already in the form.
- **The functional factory gained `TestServices`** (`ConfigureTestServices` on the base class) so a
  test can replace a service. The forgot password page got a functional test as well as sign up.
- **The regression check was run:** with the call disabled, the three refusal unit tests and the
  sign up functional test failed; restored, they pass.
- **Checked in the browser** with the default verifier: sign up reaches the dashboard and forgot
  password reaches the recovery preview. The test user was deleted afterwards.

## What

A hook that lets a host put a bot check on the forms that act for an anonymous caller: sign up, and
the host's own forgot password page. IAM ships the hook and a default that does nothing. The host
brings the provider (Cloudflare Turnstile, hCaptcha, reCAPTCHA or its own), its keys, its widget and
its content security policy. A host that adds nothing sees no change.

IAM knows no provider. It takes no new package, no provider settings and no outbound HTTP call, so
the library stays as stable as it can be and a provider's API changing never touches it.

## Why a hook in IAM at all

The sign up page belongs to `Corely.IAM.Web`. Without a hook a host can only add a check by
overriding the whole page, which copies it and drifts. DocsToData is the first host asking
(`DocsToData/Plans/New/sign-up-human-check.md` there), and it implements Turnstile against this hook.

## Scope

| Flow | Checked | Why |
|---|---|---|
| Sign up with a password (`/register`, IAM's page) | Yes, by IAM calling the verifier | Bulk account creation is the main abuse |
| Forgot password (the host's page) | Yes, by the host calling the verifier | Each request sends an email: without a check it is a mail bomb paid for by the host |
| Sign up with Google | No | Google has already verified a Google account; the token is the proof |
| Invitation acceptance | No | The invitation token is a secret only the invitee holds |
| Sign in | Not in this plan | See "Later" |

## Design

### The verifier

Public, in `Corely.IAM.Web`:

```csharp
public interface IHumanVerifier
{
    Task<VerifyHumanResult> VerifyAsync(HttpContext httpContext, string action, CancellationToken ct);
}
```

`VerifyHumanResult` carries a `VerifyHumanResultCode`: `Success`, `MissingToken`, `Rejected`,
`Unavailable`. A failed check is an outcome the page handles, so it is a result code, not an
exception (see "Result Pattern" in `CLAUDE.md`). The verifier reads its own token from the posted
form, so IAM never knows the field name. `action` names the form (`"register"`,
`"forgot-password"`) so a verifier can refuse a token solved on another form.

`NoHumanVerifier`, internal, always returns `Success`. `AddIAMWeb` registers it with `TryAdd`, so a
host's own registration wins whichever order they run in.

### The widget slot

`Pages/Authentication/_HumanCheck.cshtml` in `Corely.IAM.Web`, empty, rendered inside the sign up
form above the submit button. A host overrides it by adding a file at the same path, as the WebApp
and the demo hosts already override `_SignInAssist.cshtml` and `_VerifyMfaAssist.cshtml`. The
partial receives the action name, so the host's widget can pass it to its provider.

### The pages

1. `RegisterModel.OnPostAsync` calls `VerifyAsync(HttpContext, "register", ...)` before
   `RegisterUserAsync`. On anything but `Success` it returns the page with an error and registers
   nothing. Messages: "Please complete the check and try again." for `MissingToken` and `Rejected`,
   "We could not run the check. Please try again." for `Unavailable`.
2. `Corely.IAM.WebApp`'s `ForgotPassword.cshtml(.cs)` calls the verifier the same way with
   `"forgot-password"` and renders the partial, as the worked example of a host page. With the
   default verifier it always passes.

Check the interaction with `data-busy-spinner` on the sign up form: a host's widget must be able to
put its token in the form before the submit goes.

## What the docs tell a host

A section in `Corely.IAM.Web/Docs/security.md`, "Adding a human check":

1. Implement `IHumanVerifier` and register it as a scoped service.
2. Override `Pages/Authentication/_HumanCheck.cshtml` with the provider's widget.
3. Call the verifier from the host's own forms (forgot password at least) and render the partial
   there.
4. Add the provider to the host's content security policy (Turnstile:
   `script-src` and `frame-src` `https://challenges.cloudflare.com`).
5. What a verifier must check, so each host gets it right:
   - the provider's verdict, and also the hostname and the action it reports, so a token solved on
     another site or another form is `Rejected`; behind a proxy, the hostnames to accept come from
     configuration, not the request's `Host`;
   - fail closed: a timeout, an error status or an unreadable reply is `Unavailable`, and the page
     refuses the post; request cancellation rethrows;
   - never log the token or the secret;
   - pass the client IP only with forwarded headers configured, or it is the proxy's.
6. Mention the provider in the host's privacy policy: the widget sends visitor data to it.
7. Add rate limiting (`AddRateLimiter`) on sign in, sign up and forgot password. That is the host's,
   like security headers, and is the main defence for sign in.

`Corely.IAM.Web/Docs/setup.md` gets one line pointing there.

## Tests

Unit tier, `Corely.IAM.Web.UnitTests`:

1. `RegisterModel`: each non success code returns the page with its message and never calls
   `RegisterUserAsync`; `Success` registers as today; the verifier is asked with `"register"`.
2. `NoHumanVerifier` returns `Success`.
3. `AddIAMWeb` registers `NoHumanVerifier` when the host registered nothing, and leaves a host's
   verifier in place, before or after `AddIAMWeb`.
4. `PublicSurfaceTests` passes with the new public types reachable.

Functional tier, `Corely.IAM.Web.FunctionalTests`, with `IHumanVerifier` replaced:

5. A post to `/register` refused by the verifier creates no user.

A host's `_HumanCheck.cshtml` replacing IAM's is Razor's own override, which the sign in and MFA
pages already rely on, so it gets no test of its own here; the host checks its widget by hand.

Prove 1 and 5 catch the regression: remove the verify call, watch them go red, restore.

## Release

Additive to `Corely.IAM.Web`; it rides with the unreleased 6.0.0. `Corely.IAM` is unchanged. No
migration note: a host that adds nothing sees no change.

## Later

Sign in after repeated failures: show the check once an IP or a username has failed a few times
recently. `SecurityPolicy` already locks an account after `MaxLoginAttempts`, but that is per user
and does nothing against one IP trying many usernames. It needs a failure count keyed by IP, which
is a design of its own. Add it to `Plans/Feature-Ideas.md` when this plan ships.

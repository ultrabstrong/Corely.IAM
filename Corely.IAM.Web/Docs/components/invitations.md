# Invitations

## AcceptInvitation

**Parameters**: none. It reads the `token` query string itself, so an invitation link can carry the token to whatever path the host gives the page. Not account scoped.

Invitation acceptance page.

**Base class**: `AuthenticatedPageBase`

**Features:**
- **From a link** (`?token=` present): "You've been invited", the signed in user's email, and an Accept invitation button. No token box. The token is dropped from the address bar once read
- **Without a link**: a token box and an Accept button, for a token pasted by hand
- **Status messages**: dismissible alerts for success/error

A signed out visitor is sent to sign in with the link as the return URL. Sign up keeps it too (the
Sign Up tab and the register form carry `returnUrl`), so a new user lands back on the invitation.

**Behavior:**
- Calls `IInvitationService.AcceptInvitationAsync()` with the token
- On success: switches into the account and lands on `IIamWebLinks.Dashboard`
- Error messages mapped from result codes:
    - `InvitationExpiredError` → "This invitation has expired. Ask for a new one."
    - `InvitationRevokedError` → "This invitation has been revoked."
    - `InvitationAlreadyAcceptedError` → "This invitation has already been used."
    - `InvitationNotFoundError` → "Invitation not found. Check that you opened the whole link." (or, for a pasted token, "Check the token and try again.")
    - `EmailMismatchError` → "This invitation is for a different email address. You are signed in as x@example.com."

## Invitation Management

Invitations are managed from the [Account Detail](accounts.md) page, not from a standalone page. See the Invitations section in that doc for create, search, filter, and revoke functionality.

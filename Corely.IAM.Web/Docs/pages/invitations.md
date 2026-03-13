# Invitations

## AcceptInvitation — `/accept-invitation`

Token-based invitation acceptance page.

**Base class**: `AuthenticatedPageBase`

**Features:**
- **Token input** — text field, pre-populated from `?token=` query parameter
- **Accept button** — with loading spinner during processing
- **Status messages** — dismissible alerts for success/error

**Query parameters:**
- `token` — invitation token (optional, pre-fills the input)

**Behavior:**
- Calls `IRegistrationService.AcceptInvitationAsync()` with the token
- On success: redirects to account switching page
- Error messages mapped from result codes:
    - `InvitationExpiredError` → "This invitation has expired."
    - `InvitationRevokedError` → "This invitation has been revoked."
    - `InvitationAlreadyAcceptedError` → "This invitation has already been used."
    - `InvitationNotFoundError` → "Invitation not found. Check the token and try again."
    - `EmailMismatchError` → "This invitation is for a different user."

## Invitation Management

Invitations are managed from the [Account Detail](accounts.md) page, not from a standalone page. See the Invitations section in that doc for create, search, filter, and revoke functionality.

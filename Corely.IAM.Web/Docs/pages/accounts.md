# Accounts

## AccountDetail — `/accounts/{Id:guid}`

Account detail page with invitation management, user listing, and encryption key display.

**Base class**: `EntityDetailPageBase`

**Features:**

### Details Section
- **Edit** — account name (toggleable edit mode)
- **Delete** — confirmation modal, redirects to dashboard

### Invitations Section
- **Search** — email, description (debounced with cancellation)
- **Sort** — email, description, created date, expiry date
- **Filter** — status dropdown (All, Pending, Accepted, Revoked, Expired)
- **Pagination** — 10 per page
- **Create** — modal form with email, description, expiry duration (1h/1d/7d/30d)
- **Revoke** — available for pending invitations only
- **Status badges** — Pending (green), Accepted (blue), Revoked (red), Expired (gray)
- **Token display** — one-time token shown after creation with "Copy to Clipboard" button

### Users Section
- Paginated (10 per page), view/remove individual users

### Encryption/Signing Panel
- Symmetric encryption, asymmetric encryption, and asymmetric signature provider information

**Authorization gates:**
- `AuthAction.Update` + `ACCOUNT_RESOURCE_TYPE` + `ResourceIds: [Id]` — Edit, Create Invitation, Revoke, Remove User
- `AuthAction.Delete` + `ACCOUNT_RESOURCE_TYPE` + `ResourceIds: [Id]` — Delete
- `AuthAction.Read` + `ACCOUNT_RESOURCE_TYPE` + `ResourceIds: [Id]` — Encryption/Signing panel

**Behavior:**
- Account name changes update `IAccountDisplayState` to reflect in the NavBar
- Removing the logged-in user redirects to `/select-account`
- Deleting the account redirects to dashboard with full page reload
- Invitation token is shown once on creation — it cannot be retrieved again
- Key providers loaded asynchronously from `IRetrievalService`

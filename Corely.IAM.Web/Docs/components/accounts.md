# Accounts

## AccountDetail

**Parameters**: none; it shows the current account, never an id from the route. Wrap it in `AccountScope` in the shell; see [Hosting pages](../hosting-pages.md).

Account detail page with invitation management, user listing, and encryption key display.

**Base class**: `EntityPageBase`

**Features:**

### Details Section
- **Edit**: account name (toggleable edit mode)
- **Delete**: confirmation modal, then the host's `IIamWebLinks.Dashboard`

### Invitations Section
- **Search**: email, description (debounced with cancellation)
- **Sort**: email, description, created date, expiry date
- **Filter**: status dropdown (All, Pending, Accepted, Revoked, Expired)
- **Pagination**: 10 per page
- **Create**: modal form with email, description, expiry duration (1h/1d/7d/30d)
- **Revoke**: available for pending invitations only
- **Status badges**: Pending (green), Accepted (blue), Revoked (red), Expired (gray)
- **After creating**, in `ShownOnceModal`: when the host's `IInvitationSender` sent it, "Invitation sent" and the sender's message. Otherwise the accept link (`IIamWebLinks.AcceptInvitation()` with `?token=`) once, with Copy, who it is for and when it expires; a failed send says so first

### Users Section
- Paginated (10 per page), view/remove individual users

### Audit Section
- `AuditSettingsSection`: which actions are recorded for account members and for platform members, and how many days entries are kept
- Actions the platform does not allow are shown disabled; retention is limited to the platform's maximum
- **Edit** saves through `UpdateAccountSettingsAsync`

### Encryption/Signing Panel
- Symmetric encryption, asymmetric encryption, and asymmetric signature provider information
- **Rotate key** on each tab calls `RotateAccountKeyAsync`, or `RotateCompromisedAccountKeyAsync` when the old key is marked compromised, and reloads the providers
- The version dropdown on each tab shows an older version, whose **Mark as compromised** calls `MarkAccountKeyCompromisedAsync` and reloads the providers
- **Copy key** on each tab calls `RevealAccountKeyAsync`; see [EncryptionSigningPanel](encryption-signing.md)

**Authorization gates:**
- `AuthAction.Update` + `Account.RESOURCE_TYPE` + `ResourceIds: [Id]`: Edit, Create Invitation, Revoke, Remove User, Rotate key, Mark as compromised
- `AuthAction.Delete` + `Account.RESOURCE_TYPE` + `ResourceIds: [Id]`: Delete
- `AuthAction.Read` + `Account.RESOURCE_TYPE` + `ResourceIds: [Id]`: Encryption/Signing panel
- `AuthAction.Execute` + `Account.SECRET_KEYS_RESOURCE_TYPE` + `ResourceIds: [Id]`: Copy key, in the account or through the platform account
- `AuthAction.Read` + `AccountAuditSettings.RESOURCE_TYPE`: Audit section
- `AuthAction.Update` + `AccountAuditSettings.RESOURCE_TYPE`: Audit section Edit

**Behavior:**
- Account name changes update `IAccountDisplayState` to reflect in the NavBar
- Removing the logged-in user redirects to `/select-account`
- Deleting the account goes to `IIamWebLinks.Dashboard` with full page reload
- An invitation link is shown once on creation. It cannot be retrieved again
- Key providers loaded asynchronously from `IRetrievalService`

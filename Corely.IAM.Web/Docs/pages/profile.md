# Profile

## Profile — `/profile`

Self-service user profile page. No permission gates — any authenticated user can access their own profile.

**Base class**: `EntityPageBase`

**Features:**

### Profile Section
- **Edit fields** — username, email (toggleable edit mode)
- **Delete account** — confirmation modal, redirects to `/signout`

### Accounts Section
- Lists all accounts from `UserContext.AvailableAccounts` (in-memory, no server call)
- Paginated (10 per page)
- **View** — navigates to account detail
- **Leave** — confirmation modal, removes user from account, redirects to `/select-account`
- Only displayed when the user belongs to at least one account

### Two-Factor Authentication Section
- **Disabled state** — shows "Enable Two-Factor Authentication" button
- **Setup phase** — QR code (via JS interop with qrcodejs), secret display, recovery codes, confirmation input
- **Enabled state** — status with remaining recovery codes count, regenerate button, disable with code confirmation
- Uses `IRegistrationService` for enable/confirm/disable/regenerate and `IRetrievalService` for status

### Linked Accounts Section
- Shows linked Google email when Google auth is active
- **Unlink** — confirmation modal, calls `IDeregistrationService.UnlinkGoogleAuthAsync()`
- Placeholder when no Google account linked
- Uses `IRetrievalService.GetAuthMethodsAsync()` for status

### Encryption/Signing Panel
- User's symmetric encryption provider
- User's asymmetric encryption provider
- User's asymmetric signature provider

**Behavior:**
- No authorization gates — this is a self-service page
- Deleting user calls `DeregisterUserAsync()` and redirects to sign-out
- Leaving account calls `LeaveAccountAsync(accountId)` and redirects to account selection
- Key providers loaded asynchronously from `IRetrievalService`
- Dashboard button only shown when `UserContext?.CurrentAccount != null`

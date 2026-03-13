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

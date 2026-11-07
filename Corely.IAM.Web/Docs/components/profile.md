# Profile

## Profile

**Parameters**: none. Not account scoped, so its shell needs no `AccountScope`.

Self-service user profile page. No permission gates: any authenticated user can access their own profile.

**Base class**: `EntityPageBase`

**Features:**

### Profile Section
- **Edit fields**: username, email (toggleable edit mode)
- **Delete account**: confirmation modal, redirects to `/signout`

### Accounts Section
- Lists the accounts the user belongs to from `UserContext.MemberAccounts` (in-memory, no server call), so a platform member never sees Leave on an account they only entered
- Paginated (10 per page)
- **View**: navigates to account detail
- **Leave**: confirmation modal, removes user from account, redirects to `/select-account`
- Only displayed when the user belongs to at least one account

### Two-Factor Authentication Section
- **Disabled state**: shows "Enable Two-Factor Authentication" button
- **Setup phase**: QR code (via JS interop with qrcodejs), secret display, recovery codes, confirmation input
- **Enabled state**: status with remaining recovery codes count, regenerate button, disable with code confirmation
- Uses `IMfaService` for enable/confirm/disable/regenerate/status

### Password Section
- **Has password**: "Password is set" with "Change Password", plus "Remove Password" when an identity provider is linked
- **No password (identity providers only)**: "Set Password" form (new password + confirm)
- Change and remove ask for the current password, and every form asks for an authenticator code when TOTP is enabled
- Set calls `IRegistrationService.RegisterPasswordAsync()`, change calls `IModificationService.ModifyPasswordAsync()`, remove calls `IDeregistrationService.DeregisterBasicAuthAsync()`

### Linked Identities Section
- One row per enabled identity provider, with the linked email or "Not linked"; hidden when no provider is on
- **Link**: a full page round trip through `/signin/start/{provider}?purpose=link`, back to this page
- **Unlink**: confirmation modal, calls `IFederatedIdentityService.UnlinkFederatedIdentityAsync()`
- Takes `LinkedIdentities` from `IFederatedIdentityService.GetAuthMethodsAsync()`

### Encryption/Signing Panel
- User's symmetric encryption provider
- User's asymmetric encryption provider
- User's asymmetric signature provider
- **Rotate key** on each tab calls `RotateCurrentUserKeyAsync(keyType)` and reloads the providers

**Behavior:**
- No authorization gates; this is a self-service page
- Deleting user calls `DeregisterUserAsync()` and redirects to sign-out
- Leaving account calls `DeregisterUserFromAccountAsync(new DeregisterUserFromAccountRequest(UserContext.User.Id, accountId))` and redirects to account selection
- Key providers loaded asynchronously from `IRetrievalService`
- Dashboard button (`IIamWebLinks.Dashboard`) only shown when `UserContext?.CurrentAccount != null`; each account in the list links to `IIamWebLinks.Account`

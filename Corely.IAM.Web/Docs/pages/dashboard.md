# Dashboard

## Dashboard — `/`

Landing page after authentication and account selection. Serves as the navigation hub for all management pages.

**Route**: `/` (root)

**Render mode**: `InteractiveServerRenderMode(prerender: false)`

**Behavior:**
- Displays the current account name and navigation links to all management pages
- Unauthenticated users are redirected to `/signin` by `AuthenticatedPageBase`
- Users without an active account context see a limited view

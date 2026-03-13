# CSS & Theming

Corely.IAM.Web builds on Bootstrap 5 with minimal custom CSS for layout and component styling.

## CSS File

`wwwroot/css/iam-web.css` — auto-served from `_content/Corely.IAM.Web/css/iam-web.css`.

## Key Utility Classes

| Class | Purpose |
|-------|---------|
| `.auth-container` | Vertical centering (15vh margin-top) for auth pages |
| `.auth-card` | Centered card with shadow and rounded corners for sign-in/register forms |
| `.management-header` | Flexbox header with title + action buttons for management pages |
| `.props-grid` | CSS Grid for property label/value pairs on detail pages |
| `.section-bar` | Flexbox header for relation tables (Users, Roles, etc.) |
| `.permission-badge` (`.active` / `.inactive`) | CRUDX status indicators — green for granted, gray for not |
| `.sys-badge` / `.user-badge` | System-defined (gray) vs user-defined (blue) entity badges |
| `.entity-card` | Hover-animated cards with translateY and shadow transition |
| `.icon-btn` | Small 28x28px icon buttons for table row actions |
| `.guid-text` | Monospace, small, muted styling for GUID display |
| `.ep-*` | Effective permissions panel styles (grant rows, via badges, CRUDX mini-labels) |
| `.danger-zone` | Red-bordered section for destructive actions |
| `.dashboard-account-header` | Dark gradient header for account display |

## Table Enhancements

- `th.sortable` — clickable column headers with hover effect
- `th.sortable.active` — blue text for active sort column
- `.sort-icon` — chevron icons (up/down/expand) for sort state
- `.th-search` — compact search inputs in table headers
- `.table-actions` — right-aligned action column with nowrap

## Auth Page Styling

Auth pages use a tabbed card layout:
- `.auth-card .nav-tabs` — full-width tabs with bottom border highlight
- Active tab: blue bottom border, transparent background
- Tab items centered with `flex: 1`

## Customization

Override CSS variables or add a custom stylesheet after the IAM.Web reference:

```html
<link rel="stylesheet" href="_content/Corely.IAM.Web/css/iam-web.css" />
<link rel="stylesheet" href="css/custom-overrides.css" />
```

## Notes

- Bootstrap 5 is the primary framework — most styling uses Bootstrap utility classes directly
- Dark mode is not currently supported
- The CSS file is approximately 450 lines
- Blazor reconnect overlay styles are included for connection loss handling

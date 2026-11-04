# "Your access" in a drawer

The DocsToData owner, using the portal day to day (2026-11-03): the "Your access" card sits at the
bottom of nearly every page, always open, and can't be put away. It's useful when you need it and
noise the rest of the time.

## Where it lives

1. `ResourceAccessPanel` (a resource type's pages: lists, settings, audit) and
   `EffectivePermissionsPanel` (one item's detail page: a user, group, role, permission, API key or
   account) render the card. Both are in `Corely.IAM.Web/Components/Shared`.
2. Corely.IAM.Web uses them on 15 of its own pages; DocsToData's portal uses `ResourceAccessPanel` on
   17 more (templates, workflows, jobs, Run Once, grants, usage, SFTP, platform settings).

Because every page goes through these two components, changing them changes every page in both
products, with no page edits.

## The change

1. **The panel becomes a button that opens a drawer.** A small "Your access" button, in the same
   place on every page (the page header, beside the page's own actions), opens a Bootstrap offcanvas
   drawer from the right holding exactly what the card shows today: the C R U D X badges and what
   grants each. On a phone the drawer takes the full width.
2. **The button says something at a glance.** It shows the access level in brief (for example the
   lit badges, or "Read only"), so a person sees what they can do without opening it, and opens the
   drawer for the why.
3. **Closed by default, everywhere.** No per-user preference to remember; one consistent behavior
   is simpler than a toggle.
4. **Keyboard and screen readers:** the drawer is a real dialog, focus moves into it and back to the
   button on close, Escape closes it, and the button has an accessible name.
5. **Hosts get it for free.** Keep both components' parameters, so DocsToData takes the new version
   with no page changes; check that the button fits each host page's header, and adjust any that
   place the panel somewhere unusual.

## Tests

The existing `ResourceAccessPanelTests` and the detail page tests that find "Your access" change to
find the button and the drawer's contents. A browser test opens and closes the drawer on desktop and
phone widths.

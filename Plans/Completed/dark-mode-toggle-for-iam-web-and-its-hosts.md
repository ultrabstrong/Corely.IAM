# Dark mode toggle for Corely.IAM.Web and its hosts

## Where this comes from

`Corely.Billing.Demos.Portal` has a moon/sun button in its navbar that switches the whole app between
light and dark, and it looks good. It works because every Corely.Billing.Web component draws its
colors from Bootstrap 5.3's CSS variables (`--bs-body-bg`, `--bs-border-color`, `--bs-primary`,
`--bs-*-bg-subtle`, `--bs-secondary-color`), so setting `data-bs-theme="dark"` on an ancestor
switches them all at once. Its charts read their colors from CSS custom properties and redraw when
the attribute changes. Read these first:

- `Corely.Billing.Demos.Portal/Components/Layout/PortalLayout.razor`: the button
- `Corely.Billing.Web/Components/*.razor.css`: styling from Bootstrap variables only
- `Corely.Billing.Web/Components/UsageChart.razor.css` and `.razor.js`: chart tokens for light and dark,
  and the `MutationObserver` redraw
- `Corely.Billing.Web/Docs/styling.md`

The demo's version is a demo: the theme sits on a wrapper `div`, is Blazor state, and is lost on
reload. The scroll area outside the wrapper stays white in dark mode. The real version fixes all of
that.

## Where IAM.Web is today

- `Docs/styling.md` says "Dark mode is not currently supported".
- `wwwroot/css/iam-web.css`: 470 lines, 55 hard-coded colors, no Bootstrap variables.
- 3 Razor files use light-only classes (`bg-light`, `bg-white`, `text-dark`, `navbar-light`, `bg-dark`).
- The sign-in pages are Razor Pages under `_AuthLayout.cshtml`, not Blazor, so the theme has to work
  there without a circuit.

## Deliverables

1. **The theme lives on `<html>`.** A small script, `wwwroot/js/theme.js`, sets `data-bs-theme` on
   `document.documentElement` from `localStorage`. If nothing is saved, it follows
   `prefers-color-scheme`. The script loads in `<head>`, before first paint, so a dark user never
   sees a white flash, on the Razor Pages or in Blazor.
2. **A `ThemeToggle` component** in IAM.Web: the moon/sun button. It calls the script to flip the
   theme and save the choice, and `aria-label` names the theme it will switch to. The auth layout
   gets a plain-HTML version of the same button.
3. **`iam-web.css` on Bootstrap variables.** Replace every hard-coded color with a `--bs-*` variable,
   or with a `--iam-*` token that has a dark value under `[data-bs-theme="dark"]`. Replace the
   light-only utility classes with theme-aware ones (`bg-body`, `bg-body-tertiary`, `text-body`,
   `text-body-secondary`).
4. **Hosts opt in with two lines:** the script in `<head>` and `<ThemeToggle />` in their navbar.
   IAM's own WebApp and both demos do.
5. **Docs:** replace the "not supported" line in `styling.md` with how the theme is set, stored and
   overridden.

## Checks

- A bUnit test that `ThemeToggle` calls the theme script and labels its next state.
- Look at every page in both themes: sign-in, register, MFA, account picker, dashboard, every list
  and detail page, and the modals. Check contrast in dark mode, and focus rings too.
- A reload keeps the choice. A new browser with the OS set to dark starts dark. No white flash on
  the sign-in page.

## Afterwards

Corely.Billing.Web already follows `data-bs-theme`, so an IAM host gets both libraries in dark mode
from one toggle. DocsToData has its own plan for the admin portal, and it waits for this one.

## Outcome

All five deliverables are in. `theme.js` loads in `<head>` of every IAM layout and host, `ThemeToggle`
sits in the IAM.Web navbar and both demos' navbars, and the Razor Pages layouts (auth, legal, both
demos' auth) carry the plain-HTML button. `iam-web.css` draws from `--iam-*` tokens whose light values
are the original colors, so light mode is unchanged; the colors left as literals are the dark
surfaces, shadows and overlays that look the same in both themes. In dark mode the navbar and footer
get a border so they don't merge with the page. The WebApp's own `app.css` background follows the
theme too.

Checked in the running WebApp, in both themes: sign-in, sign-up, the no-account dashboard, the
dashboard, the permissions list and a role detail page with the effective permissions panel. A new
browser with the OS in dark mode starts dark, the choice survives reloads and navigation, and the
toggle's label names the theme it switches to. `ThemeToggleTests` covers the component.

The rest was looked at afterwards, in dark: the create, confirm and entity-picker modals, the
MFA verify page, the account picker, the profile page with an authenticator being enrolled, the
phone layout (the toggle sits in the collapsed menu), and both demos' sign-in and Blazor pages. One
fix came out of it: Bootstrap's `text-danger` on the dark modal measured about 3.5:1, below the 4.5:1
text minimum, so dark mode draws red prose (`p`, `h2`, `code`) in Bootstrap's danger emphasis color
(#ea868f, about 6.7:1). Icons keep the stock red, which clears the 3:1 bar for non-text.

# Corely.IAM.Web layout nits

Small fixes for the next round of Corely.IAM.Web changes, from the owner's review in DocsToData.

## What gets built

1. **Center the TOTP QR code at tablet widths.** In `Components/Shared/TotpSection.razor`, around
   1100px wide, where the section spans the card, the QR code sits left of center while the recovery
   codes below it are centered. Center it at every width where the recovery codes are centered, so
   both share one center line, and leave the other widths as they are.
2. **Space above "Your access".** Give `.iam-access` (`EffectivePermissionsPanel.razor`) a
   `margin-top` of 1rem in `iam-web.css`. It sets only a bottom margin, so the space above it depends
   on whatever the page put before it, and a card with no bottom margin leaves it touching. DocsToData
   carries the same rule in its `app.css` until this ships; remove it there when it takes this release.
3. Check desktop, tablet and phone widths in the consumer before tagging, as for any Blazor change.

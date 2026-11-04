# Two-factor setup usability

Found by the DocsToData owner enabling two-factor authentication on a new environment's platform
owner (2026-11-03), with 1Password as the authenticator. All five are in `TotpSection.razor` and what
it calls; none is a security flaw, but each one makes a person stall or give up during setup.

## What goes wrong

1. **The secret has no reveal or copy.** It is printed in plain text beside the QR code. It should be
   hidden by default, with an eye button to show it and a copy button, the way the key pages show a
   new key.
2. **The recovery codes have no copy.** A person has to select ten codes by hand. One copy button
   should copy all of them, one per line.
3. **1Password could not scan the QR code.** Likely causes, to confirm before changing anything: it
   is drawn at 150 pixels by `qrcodejs`, which leaves no quiet zone (the white border scanners need),
   and in dark mode the page around it is dark. Draw it on a white padded box at a larger size, and
   prove it scans with 1Password and at least one phone authenticator, in light and dark.
4. **1Password didn't recognize the confirmation box.** It is a bare `type="text"` input with only a
   placeholder. It needs `autocomplete="one-time-code"`, `inputmode="numeric"`, a `name` and a real
   label, so password managers offer to fill it. The disable box below it has the same problem.
5. **Enabling took about ten seconds with no sign of work.** The button only turns disabled. Two
   parts:
   1. **Show progress at once:** a spinner and "Setting up…" on the button while it works, as the
      authentication forms do.
   2. **Find what takes ten seconds.** The likely cause is `GenerateRecoveryCodesAsync` in
      `TotpAuthProcessor`: for each code it runs the configured hash, then saves it with its own
      `CreateAsync`, so ten codes are ten hashes and ten database round trips in a row. Measure
      first. If that's it, save the codes together and consider whether recovery codes, which are
      long random values, need the same deliberately slow hash as a password. That is a security
      call to make explicitly, not a side effect of a speed fix.

Regenerating recovery codes goes through the same `GenerateRecoveryCodesAsync` and shares items 2
and 5.

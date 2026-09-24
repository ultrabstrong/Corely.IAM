# Unattended work queue

Owner-approved order for a session running without the owner. On resume, read this first, then the
plan named in the first unfinished item. Tick an item only when it is committed and pushed.

Push each repository's default branch when its build and tests are green. Never publish packages,
bump versions or push tags.

- [ ] **Seams rule, Corely repositories.** `Plans/New/move-private-conversions-into-tested-members-and-extensions.md`
  in this repository. Order: Common, DataAccess, Security, IAM.
- [ ] **Seams rule, DocsToData.** `C:\source\git\pinnacleinnovation\DocsToData\Plans\New\mapping-extensions.md`.
- [ ] **Corely.Billing `this T` sweep.** Possibly already done by another session; check `git log`
  first and skip if so. Otherwise convert every `this T` to an `extension(T)` block and drop
  "(existing ones convert when next touched)" from its `CLAUDE.md`.
- [ ] **DocsToData comment cleanup.** `DocsToData\Plans\New\comment-cleanup.md`, but judge comments by
  the rule in this repository's `Plans/Completed/remove-all-comments.md` and commit `644effdd`
  (keep only comments that survive a check against the code). The owner wants that rule everywhere;
  where DocsToData's plan or `CLAUDE.md` differs, follow this repository's and update theirs to match.
- [ ] **DocsToData cancellation audit.** `DocsToData\Plans\New\cancellation-audit.md`. Fix dropped
  tokens; for the Blazor page cancellation question, record the options in the plan, do not decide.

Not to be picked up: `per-package-release-tags`, Billing's `iam-permissions-package`,
`github-access-review`, `recreate-sftp-logins`, `corely-billing-web-and-unlimited-grants`.

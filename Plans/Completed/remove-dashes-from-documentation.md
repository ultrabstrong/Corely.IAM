# Remove dashes from the documentation

The documentation across the Corely repositories and DocsToData leans on em dashes, which reads as
machine-written. Take out every em dash, en dash and hyphen-used-as-a-dash that is not needed, and
change the style guides so they stop coming back.

## Scope

Markdown documentation in these repositories:

| Repository | Em dashes | Hyphens used as dashes |
|---|---|---|
| Corely.IAM | 445 | 40 |
| Corely.Billing | 119 | 17 |
| DocsToData | 29 (and 1 en dash) | 10 |
| Corely.Common | 18 | 3 |
| Corely.Security | 14 | 12 |
| Corely.DataAccess | 11 | 2 |

"Documentation" is every `.md` file except:

- `Plans/`: working material, not documentation.
- `CLAUDE.md`: instructions to agents, not documentation.
- Anything generated or vendored (`node_modules`, `bin`, `obj`).

Left out on purpose: Logseq (personal notes, not published), and Raspberry-Pi, Rental, SeqApps and
TheSandbox (a README each, no documentation section).

Only documentation changes. No code, no XML doc comments, no version bumps: nothing here changes a
package, so nothing is republished.

## What counts as unnecessary

Remove:

- **Em dash (`—`)** everywhere in prose.
- **En dash (`–`)**: a range becomes "to" (`1–5` becomes `1 to 5`).
- **A hyphen or double hyphen used as a dash**: ` - ` or ` -- ` between two parts of a sentence.
- **A dash standing in for an empty table cell**: write what it means ("none", "n/a"), or leave the
  cell empty.

Keep:

- Hyphens inside words (`host-agnostic`, `read-only`, `step-by-step`).
- Anything in code: fenced blocks, inline code, CLI flags (`--project`), file names, identifiers.
- Markdown syntax: list markers, table separator rows, horizontal rules.

## How each dash is rewritten

Each dash is rewritten by reading the sentence, not by swapping in one character. A mechanical
replacement leaves the same machine rhythm with a different character in it.

| The dash is doing | Becomes |
|---|---|
| Joining a bold lead to its description (`- **Lead** — text`) | A colon: `- **Lead**: text` |
| Introducing an explanation or a list | A colon, or a new sentence |
| Setting off an aside in the middle of a sentence | Commas, or parentheses when the aside is long |
| Adding an afterthought | A new sentence, or cut it if it adds nothing |
| Marking a contrast ("X — but Y") | A comma or a new sentence |

When a sentence only reads well with the dash, rewrite the sentence.

## The style guides

Corely.IAM's and Corely.Billing's `DOCUMENTATION-STYLE.md` prescribe the em dash: "Bold lead with
em-dash description", and "Em-dash for inline elaboration within bullet points". Change both, first:

- The bold-lead pattern becomes `- **Bold lead**: description`.
- Add a rule: no em or en dashes, and no hyphen used as a dash; use a colon, a comma, parentheses or
  a new sentence.

Corely.Common, Corely.DataAccess, Corely.Security and DocsToData have no style guide, so nothing to
change there.

## A rule in every CLAUDE.md

Every in-scope repository's `CLAUDE.md` gets the rule too, so agents writing new documentation, plans
or comments stop producing dashes: no em dash, no en dash, no hyphen used as a dash, in anything
written; use a colon, a comma, parentheses or a new sentence. This covers all six repositories,
including the four with no style guide.

## Order

One repository at a time, one commit each, pushed when done:

1. Corely.IAM: style guide first, then the rest.
2. Corely.Billing: the same.
3. Corely.Common, Corely.Security, Corely.DataAccess.
4. DocsToData. Other sessions work in it, so pull first and commit only the documentation paths
   touched. Stay out of its worktrees.

## Check

- A search for `—` and `–` across the in-scope files finds nothing, and one for ` - ` or ` -- `
  in prose finds nothing outside code.
- The diff touches only `.md` files.
- Rendered spot check: a few changed pages read naturally, and tables and lists still render.
- Nothing needs a version bump. Corely.IAM's `check-package-versions.sh` excludes `Docs/`, but a
  package README counts as a package change there; the next real release carries the bump, and the
  script only fails at release time.

## Outcome

Done in all six repositories, one commit each, pushed: Corely.IAM `13ebf559`, Corely.Billing
`c9e73b0`, Corely.Common `d13fa9b`, Corely.Security `30eb38c`, Corely.DataAccess `1a0a84e`,
DocsToData `3a3d6cd`. Every `CLAUDE.md` carries the rule, and both style guides now use the colon.

Bold-lead bullets were converted by rule and sampled; everything else was rewritten by hand, including
dashes inside code-sample comments. Three docs were saved as Windows-1252, which GitHub showed as
garbled characters, and are now UTF-8: `The IAM Difference.md`, Common's
`request-response-handler.md` and DataAccess's `context-configuration.md`.

Left on purpose, because they are quoted rather than written here:

- DataAccess `DESIGN-RATIONALE.md`: two sentences quoted verbatim from Microsoft's documentation.
- IAM `Docs/Permission Model.md`: the default permission descriptions, which are data values.
- DocsToData `iac/docstodata-sftp/README.md`: the Azure image name, as Azure spells it.

One slip: the DocsToData commit also picked up `Plans/InProgress/github-access-review.md`, another
session's in-progress move of that plan, because it had unstaged edits. Its staged deletion of the
`Plans/New` copy still completes the move when that session commits.

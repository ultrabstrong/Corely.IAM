# Per-package release tags

## Starting cold

For a session picking this up with no history. The change spans two repositories, done in the same
way:

| Repository | Path |
|---|---|
| Corely.IAM | `C:\source\git\ultrabstrong\Corely.IAM` |
| Corely.Billing | `C:\source\git\ultrabstrong\Corely.Billing` |

Read each repository's `CLAUDE.md` before touching it. Tags are pushed only with the owner's say-so,
since a published package version cannot be deleted.

**Decided with the owner; do not reopen:** tags per package, not lockstep versioning. Each package
keeps its own `<Version>` in its csproj.

## The problem

A tag names a release of the repository, not a version of anything published. `release.yml` fires on
`v*`, packs every package, and each takes its version from its own csproj:

| Tag | Corely.IAM | Corely.IAM.Web | Migration CLI |
|---|---|---|---|
| `v2.6.0` | 2.2.2 | 2.3.0 | 2.0.1 |
| `v2.7.0` | 2.3.0 | 2.3.0 | 2.0.1 |

Nothing maps `v2.7.0` to Corely.IAM 2.3.0 without that table. Corely.Billing's `v1.0.0-preview.2`
matches its package only by coincidence; its CLI is still `1.0.0-preview.1`, so the next CLI-only
release breaks the match.

The industry convention for independently versioned packages in one repository is a tag naming both
the package and its version: Azure SDK for .NET (`Azure.Storage.Blobs_12.19.1`), OpenTelemetry .NET
(`core-1.9.0`), changesets/lerna (`pkg@1.2.3`), Go nested modules (`subdir/v1.2.3`).

## The change

Tag format `<PackageId>-v<Version>`, where `<Version>` equals the csproj's `<Version>`:

```
Corely.IAM-v2.3.0
Corely.IAM.Web-v2.3.0
Corely.IAM.DataAccessMigrations.Cli-v2.0.1
Corely.Billing-v1.0.0-preview.2
Corely.Billing.DataAccessMigrations.Cli-v1.0.0-preview.1
```

A tag releases exactly the package it names. Releasing two packages is two tags on the same commit.

**`release.yml` keeps its file name.** The nuget.org trusted-publishing policy is bound to it;
renaming breaks publishing until the policy is edited.

`--match 'Corely.IAM-v*'` does not match `Corely.IAM.Web-v*` (the character after `Corely.IAM` is `.`,
not `-`), so a plain prefix glob is enough to find a package's own previous tag.

## Work

**Corely.IAM**
1. `release.yml`:
   - Trigger on `Corely.IAM*-v*` instead of `v*`. Drop `workflow_dispatch`, which has no tag and so
     no package to release.
   - A first step splits `GITHUB_REF_NAME` on the last `-v` into package id and version, and fails
     unless the id is one of the three known packages.
   - Fail unless the tag version equals that csproj's `<Version>`. This replaces the "changed without a
     bump" check at release time: a tag that names a version already on nuget.org is now the
     mistake, and `--skip-duplicate` must not turn it green. Drop `--skip-duplicate` from the push.
   - Build and test the whole solution as now; pack and push only the named project.
   - For the migration CLI, fail unless its major equals `Corely.IAM`'s major at that commit.
2. `scripts/check-package-versions.sh` keeps its CI role (non-strict warning on push) but compares
   each package against **its own** latest tag (`git describe --match '<id>-v*'`) instead of the last
   `v*`. With no tag for a package yet, it reports and skips that package.
3. Baseline tags for what is on nuget.org today, pointed at the commits that shipped them, so step 2
   has something to compare against. Find each commit from the `v*` tag whose release first pushed that
   version. These are pushed **before** the new `release.yml` reaches master: GitHub runs the workflow
   file at the tagged commit, whose trigger is still `v*`, so a baseline tag publishes nothing.
4. `CLAUDE.md`: a short "Releasing" section — the tag format, one tag per package, tag version must
   equal the csproj version, CLI major tracks IAM's. `ci.yml`'s comment about "a version tag" updated
   to match.
5. The old `v*` tags stay. They are history and deleting published tags breaks anyone who pinned one.

**Corely.Billing** — the same five steps, with `Corely.Billing*-v*` and its two packages.

**Elsewhere**
- `C:\source\git\pinnacleinnovation\DocsToData` plans and docs that say "releasing a library is tagging
  `vX.Y.Z`" (the completed keyed-configuration plan here said it too). Search both
  `pinnacleinnovation` and `ultrabstrong` for `vX.Y.Z` and `git tag v`; fix live docs, leave completed
  plans alone.

## Verify

Without publishing anything:
- Run the tag-parsing step locally against each sample tag above, plus a malformed one
  (`Corely.IAM-2.3.0`, `Corely.Nope-v1.0.0`), and confirm each passes or fails as intended.
- Run the non-strict check after the baselines exist; every package should report `unchanged`.

The first real release under the new scheme is the proof end to end. Ask the owner before tagging it.

## Done when

Each repository releases one package per tag, the tag version equals the package version or the run
fails, every published package has a baseline tag, and the `CLAUDE.md` of each says how to release.

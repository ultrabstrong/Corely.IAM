#!/usr/bin/env bash
# Fails when a packable project changed since the last release tag without its <Version> changing.
#
# The failure this guards against is silent: `dotnet nuget push --skip-duplicate` treats an
# already-published version as success, so a forgotten bump publishes nothing and the release still
# reports success.
#
# Hard failure only when cutting a release (STRICT=1, set by the release workflow). On ordinary
# pushes it reports and exits 0 - a warning that fires on every comment edit is one people learn to
# ignore, and a version bump is only actually required at the point of publishing.
set -euo pipefail

STRICT="${STRICT:-0}"

# Project directory -> csproj, for every project this repository publishes.
PACKAGES=(
  "Corely.IAM:Corely.IAM/Corely.IAM.csproj"
  "Corely.IAM.Web:Corely.IAM.Web/Corely.IAM.Web.csproj"
  "Corely.IAM.DataAccessMigrations.Cli:Corely.IAM.DataAccessMigrations.Cli/Corely.IAM.DataAccessMigrations.Cli.csproj"
)

read_version() { # <git-ref-or-empty> <path>
  if [ -z "$1" ]; then
    sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$2" | head -1
  else
    git show "$1:$2" 2>/dev/null | sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' | head -1
  fi
}

# When run on a tag, that tag is HEAD - compare against the one before it.
CURRENT_TAG="$(git tag --points-at HEAD --list 'v*' | head -1)"
if [ -n "$CURRENT_TAG" ]; then
  TAG="$(git describe --tags --abbrev=0 --match 'v*' "${CURRENT_TAG}^" 2>/dev/null || true)"
else
  TAG="$(git describe --tags --abbrev=0 --match 'v*' 2>/dev/null || true)"
fi

if [ -z "$TAG" ]; then
  echo "No previous release tag found; nothing to compare against."
  exit 0
fi

echo "Comparing against $TAG"
stale=()

for entry in "${PACKAGES[@]}"; do
  dir="${entry%%:*}"
  csproj="${entry#*:}"

  # Docs/ is not packed, so changes there cannot reach a consumer and must not demand a release.
  if git diff --quiet "$TAG" HEAD -- "$dir" ":(exclude)$dir/Docs"; then
    echo "  $dir: unchanged"
    continue
  fi

  current="$(read_version "" "$csproj")"
  tagged="$(read_version "$TAG" "$csproj")"

  if [ "$current" = "$tagged" ]; then
    echo "  $dir: CHANGED but still $current"
    stale+=("$dir ($current)")
  else
    echo "  $dir: changed, $tagged -> $current"
  fi
done

if [ ${#stale[@]} -eq 0 ]; then
  echo "All changed packages have a new version."
  exit 0
fi

echo
echo "Changed without a version bump:"
printf '  %s\n' "${stale[@]}"
echo
echo "Publishing skips these as duplicates and still reports success, so the change reaches nobody."

if [ "$STRICT" = "1" ]; then
  echo "Bump <Version>, or revert the change if it was not meant to ship."
  exit 1
fi

echo "Not failing outside a release. Bump before tagging if these are meant to ship."
exit 0

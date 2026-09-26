#!/usr/bin/env bash
set -euo pipefail

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

stale=()

for entry in "${PACKAGES[@]}"; do
  dir="${entry%%:*}"
  csproj="${entry#*:}"

  tag="$(git describe --tags --abbrev=0 --match "$dir-v*" 2>/dev/null || true)"
  if [ -z "$tag" ]; then
    echo "  $dir: no $dir-v* tag yet, skipped"
    continue
  fi

  # Docs/ is not packed, so changes there cannot reach a consumer and must not demand a release.
  if git diff --quiet "$tag" HEAD -- "$dir" ":(exclude)$dir/Docs"; then
    echo "  $dir: unchanged since $tag"
    continue
  fi

  current="$(read_version "" "$csproj")"
  tagged="$(read_version "$tag" "$csproj")"

  if [ "$current" = "$tagged" ]; then
    echo "  $dir: CHANGED since $tag but still $current"
    stale+=("$dir ($current)")
  else
    echo "  $dir: changed since $tag, $tagged -> $current"
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
echo "Bump <Version> before tagging if these are meant to ship."

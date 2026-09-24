#!/usr/bin/env bash
set -euo pipefail

worktree_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$worktree_root"

echo "Restoring ItisDota.sln in $worktree_root"
dotnet restore ItisDota.sln

main_root="${ROOT_WORKTREE_PATH:-}"
if [[ -z "$main_root" || ! -d "$main_root" ]]; then
  echo "ROOT_WORKTREE_PATH is not set; skipped copy of local untracked files."
  echo "Worktree setup complete."
  exit 0
fi

copied=0
for name in .env secrets.json; do
  if [[ -f "$main_root/$name" ]]; then
    cp "$main_root/$name" "$worktree_root/$name"
    echo "Copied $name"
    copied=$((copied + 1))
  fi
done

while IFS= read -r -d '' source; do
  relative="${source#"$main_root"/}"
  destination="$worktree_root/$relative"
  mkdir -p "$(dirname "$destination")"
  cp "$source" "$destination"
  echo "Copied $relative"
  copied=$((copied + 1))
done < <(find "$main_root" -type f -name 'appsettings.*.local.json' -not -path '*/bin/*' -not -path '*/obj/*' -print0)

echo "Copied $copied local file(s). Worktree setup complete."

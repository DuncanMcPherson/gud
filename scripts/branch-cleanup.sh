#!/usr/bin/env bash
set -euo pipefail

DRY_RUN=false
if [[ "${1:-}" == "--dry" || "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=true
fi

BASE_BRANCH="dev"
PROTECTED="^(dev|master)$"

git checkout "$BASE_BRANCH" --quiet

for branch in $(git branch --format='%(refname:short)'); do
  if [[ "$branch" =~ $PROTECTED ]]; then
    continue
  fi
  
  diff=$(git cherry "$BASE_BASE_BRANCH" "$branch")
  
  if ! echo "$diff" | grep -q '^+'; then
    if $DRY_RUN; then
      echo "[dry-run] Would delete: $branch"
    else
      git branch -D "$branch"
      echo "Deleted: $branch"
    fi
  fi
done
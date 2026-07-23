#!/usr/bin/env bash
set -euo pipefail

DRY_RUN=false
if [[ "${1:-}" == "--dry" || "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=true
fi

IGNORED_BRANCHES=(
  "dev"
  "master"
  "gh-pages"
)
  
is_ignored_branch() {
  local branch="$1"
  
  for ignored in "${IGNORED_BRANCHES[@]}"; do
    if [[ "$branch" == "$ignored" ]]; then
      return 0
    fi
  done
}

mapfile -t LOCAL_BRANCHES < <(
git branch --format='%(refname:short)'
)
  
mapfile -t MERGED_BRANCHES < <(
gh pr list --state merged --json headRefName,baseRefName --limit 1000 | jq -r '.[] | select (.baseRefName == "dev") | .headRefName'
)
  
for branch in "${MERGED_BRANCHES[@]}"; do
  if [[ ! " ${LOCAL_BRANCHES[*]} " =~ ${branch} ]]; then
    continue
  fi
  
  if is_ignored_branch "$branch"; then
    continue
  fi
  
  if $DRY_RUN; then
    echo "[dry-run] Would delete: $branch"
  else
    git branch -D "$branch"
    echo "Deleted $branch"
  fi
done

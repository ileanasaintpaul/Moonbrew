#!/usr/bin/env bash
# Checks that every asset under Assets/ has a .meta file, and that no .meta is orphaned.
set -euo pipefail

root="${1:-Assets}"
errors=0

# Every file/folder needs a .meta (Unity ignores hidden items and names ending with ~)
while IFS= read -r -d '' path; do
  if [[ ! -e "$path.meta" ]]; then
    echo "::error file=$path::Missing .meta for $path"
    errors=$((errors + 1))
  fi
done < <(find "$root" -mindepth 1 \
  -not -name '*.meta' \
  -not -path '*/.*' \
  -not -path '*~/*' -not -name '*~' \
  -print0)

# Every .meta needs its asset
while IFS= read -r -d '' meta; do
  asset="${meta%.meta}"
  if [[ ! -e "$asset" ]]; then
    echo "::error file=$meta::Orphan .meta (no asset): $meta"
    errors=$((errors + 1))
  fi
done < <(find "$root" -name '*.meta' -not -path '*/.*' -print0)

if (( errors > 0 )); then
  echo "Found $errors .meta problem(s)."
  exit 1
fi
echo ".meta files OK"

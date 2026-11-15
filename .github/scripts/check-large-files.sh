#!/usr/bin/env bash
# Fails if a tracked file is larger than the limit without being stored in Git LFS.
# Run on a checkout made WITHOUT LFS content: LFS files are then tiny pointer files,
# so anything big that remains was committed outside LFS.
set -euo pipefail

limit_mb="${1:-10}"
limit_bytes=$((limit_mb * 1024 * 1024))
errors=0

while IFS= read -r -d '' file; do
  [[ -f "$file" ]] || continue
  size=$(stat -c %s "$file")
  if (( size > limit_bytes )); then
    echo "::error file=$file::$file is $((size / 1024 / 1024)) MB and not in Git LFS (limit ${limit_mb} MB). Add its extension to .gitattributes."
    errors=$((errors + 1))
  fi
done < <(git ls-files -z)

if (( errors > 0 )); then
  exit 1
fi
echo "No large file outside LFS (limit ${limit_mb} MB)"

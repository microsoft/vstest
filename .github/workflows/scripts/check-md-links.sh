#!/usr/bin/env bash
#
# check-md-links.sh — extract and test relative markdown links and heading anchors.
#
# This is the single, shared implementation of the link-checking logic used by both:
#   - the `md-link-checker` agentic workflow (.github/workflows/md-link-checker.md), and
#   - the local `@md-link-checker` agent (.github/agents/md-link-checker.md).
#
# Only links to other `.md` files and in-file/cross-file heading anchors are checked.
# Absolute URLs (http/https/mailto/etc.) are intentionally ignored.
#
# Usage:
#   check-md-links.sh [file.md ...]
#
#   With no arguments it scans the default pipeline scope: every *.md under docs/
#   plus the root README.md. Pass an explicit list of files (e.g. the changed docs)
#   to scope the check — handy for fast local runs.
#
# Environment:
#   OUT_DIR   Directory for output files (default: /tmp/gh-aw/agent). Created if missing.
#
# Outputs (in OUT_DIR):
#   link-check-results.md   Full human-readable report of every link tested.
#   broken-links.md         Only the broken links, one per line, with the source file.
#   all-links.txt           Raw "source_file|url" pairs (intermediate).
#   unique-links.txt        Sorted/deduped "source_file|url" pairs (intermediate).
#
# Exit code is always 0 (a broken link is a reported result, not a script failure).
# Read broken_count / working_count from the printed summary or count broken-links.md.

set -uo pipefail

OUT_DIR="${OUT_DIR:-/tmp/gh-aw/agent}"
mkdir -p "$OUT_DIR"

RESULTS="$OUT_DIR/link-check-results.md"
BROKEN="$OUT_DIR/broken-links.md"
ALL_LINKS="$OUT_DIR/all-links.txt"
UNIQUE_LINKS="$OUT_DIR/unique-links.txt"

echo "# Link Check Results" > "$RESULTS"
echo "" >> "$RESULTS"
echo "# Broken Links" > "$BROKEN"
echo "" >> "$BROKEN"
rm -f "$ALL_LINKS"

# Resolve the set of markdown files to check. Explicit arguments win; otherwise fall
# back to the pipeline default scope (docs/ + README.md).
echo "Finding all markdown files..."
collect_default_files() {
  find docs README.md -type f -name "*.md" -print0 2>/dev/null
}

if [ "$#" -gt 0 ]; then
  # Build a NUL-delimited stream from the provided file list, skipping anything
  # that is not an existing *.md file.
  for f in "$@"; do
    if [[ "$f" == *.md ]] && [ -f "$f" ]; then
      printf '%s\0' "$f"
    fi
  done > "$OUT_DIR/.files.lst0"
else
  collect_default_files > "$OUT_DIR/.files.lst0"
fi

if [ ! -s "$OUT_DIR/.files.lst0" ]; then
  echo "No markdown files found"
  rm -f "$OUT_DIR/.files.lst0"
  exit 0
fi

# Extract all links from markdown files
echo "## Links Found" >> "$RESULTS"
echo "" >> "$RESULTS"

# Use grep to find markdown links
# Format for relative links: "source_file|url" to allow path resolution
while IFS= read -r -d '' file; do
  echo "Checking $file..."
  # Extract markdown links [text](url)
  grep -oP '\[([^\]]+)\]\(([^\)]+)\)' "$file" | grep -oP '\(([^\)]+)\)' | tr -d '()' | while IFS= read -r link; do
    echo "$file|$link" >> "$ALL_LINKS"
  done 2>/dev/null || true
done < "$OUT_DIR/.files.lst0"
rm -f "$OUT_DIR/.files.lst0"

# Remove duplicates and sort
if [ -f "$ALL_LINKS" ]; then
  sort -u "$ALL_LINKS" > "$UNIQUE_LINKS"
  LINK_COUNT=$(wc -l < "$UNIQUE_LINKS")
  echo "Found $LINK_COUNT unique links" >> "$RESULTS"
  echo "" >> "$RESULTS"
else
  echo "No links found" >> "$RESULTS"
  exit 0
fi

# Helper: check if an explicit HTML anchor or markdown heading anchor exists in a file
check_anchor() {
  local file="$1"
  local anchor="$2"
  local html_anchor heading generated

  while IFS= read -r html_anchor; do
    if [[ "$html_anchor" == "$anchor" ]]; then
      return 0
    fi
  done < <(grep -oiP "<a\\b[^>]*\\b(?:name|id)\\s*=\\s*['\"]\\K[^'\"]+(?=['\"])" "$file" 2>/dev/null)

  while IFS= read -r heading; do
    generated=$(printf '%s' "$heading" | sed -E 's/<[^>]*>//g' | sed -E 's/[[:space:]]+/ /g; s/^ //; s/ $//' | tr '[:upper:]' '[:lower:]' | sed 's/ /-/g' | sed 's/[^a-z0-9_-]//g')
    if [[ "$generated" == "$anchor" ]]; then
      return 0
    fi
  done < <(grep -oP '^#{1,6}\s+\K.*' "$file" 2>/dev/null)

  return 1
}
# Test each link
echo "## Link Test Results" >> "$RESULTS"
echo "" >> "$RESULTS"
echo "Testing links..." >> "$RESULTS"

BROKEN_COUNT=0
WORKING_COUNT=0
while IFS='|' read -r source_file url; do
  if [[ "$url" == "#"* ]]; then
    # Same-file anchor link
    ANCHOR="${url#\#}"
    if check_anchor "$source_file" "$ANCHOR"; then
      WORKING_COUNT=$((WORKING_COUNT + 1))
      echo "✅ $url (anchor in $source_file)" >> "$RESULTS"
    else
      BROKEN_COUNT=$((BROKEN_COUNT + 1))
      echo "❌ $url (anchor not found in $source_file)" >> "$RESULTS"
      echo "❌ $url (anchor not found in $source_file)" >> "$BROKEN"
    fi
  elif [[ "$url" =~ ^[a-zA-Z][a-zA-Z0-9+.-]*: ]]; then
    # Skip absolute URLs (http, https, mailto, etc.) — we only check links to other .md files and anchors
    continue
  else
    # Relative file link, possibly with anchor
    # Split into file path and optional anchor
    REL_PATH="${url%%#*}"
    ANCHOR=""
    if [[ "$url" == *"#"* ]]; then
      ANCHOR="${url#*#}"
    fi
    # Resolve relative to the source file's directory
    SOURCE_DIR=$(dirname "$source_file")
    TARGET_PATH="$SOURCE_DIR/$REL_PATH"
    # Normalize the path
    TARGET_PATH=$(realpath --relative-to=. "$TARGET_PATH" 2>/dev/null || echo "$TARGET_PATH")
    if [[ -z "$REL_PATH" ]] && [[ -n "$ANCHOR" ]]; then
      # Link is just "#anchor" handled above, but in case of edge cases
      WORKING_COUNT=$((WORKING_COUNT + 1))
    elif [[ ! -f "$TARGET_PATH" ]]; then
      BROKEN_COUNT=$((BROKEN_COUNT + 1))
      echo "❌ $url (file not found: $TARGET_PATH) in $source_file" >> "$RESULTS"
      echo "❌ $url (file not found: $TARGET_PATH) in $source_file" >> "$BROKEN"
    elif [[ -n "$ANCHOR" ]]; then
      # File exists, check the anchor
      if check_anchor "$TARGET_PATH" "$ANCHOR"; then
        WORKING_COUNT=$((WORKING_COUNT + 1))
        echo "✅ $url (file + anchor OK) in $source_file" >> "$RESULTS"
      else
        BROKEN_COUNT=$((BROKEN_COUNT + 1))
        echo "❌ $url (file exists but anchor '#$ANCHOR' not found in $TARGET_PATH) in $source_file" >> "$RESULTS"
        echo "❌ $url (file exists but anchor '#$ANCHOR' not found in $TARGET_PATH) in $source_file" >> "$BROKEN"
      fi
    else
      WORKING_COUNT=$((WORKING_COUNT + 1))
      echo "✅ $url (file exists: $TARGET_PATH)" >> "$RESULTS"
    fi
  fi
done < "$UNIQUE_LINKS"

echo "" >> "$RESULTS"
echo "**Summary:** $WORKING_COUNT working, $BROKEN_COUNT broken" >> "$RESULTS"
echo "" >> "$BROKEN"
echo "**Summary:** $BROKEN_COUNT broken links" >> "$BROKEN"

# Output results to GitHub Actions step outputs when running in a workflow.
if [ -n "${GITHUB_OUTPUT:-}" ]; then
  echo "broken_count=$BROKEN_COUNT" >> "$GITHUB_OUTPUT"
  echo "working_count=$WORKING_COUNT" >> "$GITHUB_OUTPUT"
fi

cat "$RESULTS"

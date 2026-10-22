#!/usr/bin/env bash
# Runs every stage in /verify/stages in name order, always to the end, then prints a summary and exits
# non-zero if any test failed. Adding a stack (for example React) means adding one more stage script.
# No `set -u`: a missing setting must show up as failing checks, never stop the remaining stages.

export RESULTS_DIR=${RESULTS_DIR:-/results}
mkdir -p "$RESULTS_DIR"
: > "$RESULTS_DIR/latest.log"
: > "$RESULTS_DIR/latest.jsonl"

# shellcheck source=lib.sh
source /verify/lib.sh

# Only the mail check needs the provider key; keep it out of the environment of the other stages.
MAIL_KEY="${MAILERSEND_API_KEY:-}"
unset MAILERSEND_API_KEY

STARTED=$(date -u +%FT%TZ)
for stage_script in /verify/stages/*.sh; do
  # VERIFY_STAGES limits the run to the stages whose file name starts with one of these prefixes (a regular
  # expression, for example "40" or "10|20"); the default is every stage.
  if [[ -n "${VERIFY_STAGES:-}" && ! "$(basename "$stage_script")" =~ ^($VERIFY_STAGES) ]]; then
    continue
  fi
  # shellcheck disable=SC1090
  source "$stage_script"
done
FINISHED=$(date -u +%FT%TZ)

jq -s --arg started "$STARTED" --arg finished "$FINISHED" \
  '{startedAtUtc: $started, finishedAtUtc: $finished,
    passed: (map(select(.result == "PASS")) | length),
    failed: (map(select(.result == "FAIL")) | length),
    skipped: (map(select(.result == "SKIP")) | length),
    known: (map(select(.result == "KNOWN")) | length),
    tests: .}' "$RESULTS_DIR/latest.jsonl" > "$RESULTS_DIR/latest.json"

printf '\n=== SUMMARY: %s passed, %s failed, %s skipped, %s known ===\n' "$PASS" "$FAIL" "$SKIP" "$KNOWN" | tee -a "$RESULTS_DIR/latest.log"
if [[ "$FAIL" -gt 0 ]]; then
  echo "Failed tests:" | tee -a "$RESULTS_DIR/latest.log"
  jq -r 'select(.result == "FAIL") | "  \(.stage): \(.test) - \(.detail)"' "$RESULTS_DIR/latest.jsonl" | tee -a "$RESULTS_DIR/latest.log"
  exit 1
fi
exit 0

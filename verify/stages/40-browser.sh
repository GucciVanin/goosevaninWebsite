#!/usr/bin/env bash
# Stage: browser tests (TE5). Playwright runs the specs in /verify/browser/tests on Chromium, Firefox and WebKit
# against the live stack, with axe-core for accessibility. Each test of each browser becomes one log line
# (PASS, FAIL or KNOWN) in the same log and JSON as the other stages.
#   VERIFY_BROWSERS=firefox        limit the engines (comma list of chromium, firefox, webkit)
#   VERIFY_BROWSER_GREP="home"     run only tests whose name matches (Playwright --grep)
STAGE=browser
section "browser tests (Playwright) against https://localhost"
LAST_MS=$(now_ms)

browser_jsonl="$RESULTS_DIR/browser.jsonl"
browser_out="$RESULTS_DIR/browser.out"
rm -f "$browser_jsonl" "$browser_jsonl.details.log"

# Inside this container https://localhost must reach Caddy: forward port 443 on the loopback addresses to it.
forward_log=$(mktemp)
node /verify/browser/helpers/forward.cjs "$CADDY_IP" > "$forward_log" 2>&1 &
forward_pid=$!
for _ in $(seq 1 50); do
  grep -q '^ready' "$forward_log" && break
  sleep 0.1
done

# This stage counts its own failures: the global FAIL counter also holds the other stages' results.
stage_fail=0
stage_record() { # result name ms [detail]
  [[ "$1" == FAIL ]] && stage_fail=$((stage_fail + 1))
  record "$@"
}

playwright_bin=/verify/browser/node_modules/.bin/playwright
grep_args=()
[[ -n "${VERIFY_BROWSER_GREP:-}" ]] && grep_args=(--grep "$VERIFY_BROWSER_GREP")

# The test list is the source of truth for what must run. `--list` also tells each test's screen and check kind (tags).
list_json() { (cd /verify/browser && "$playwright_bin" test --list --reporter=json "${grep_args[@]}" 2>/dev/null); }
specs_jq='.. | objects | select(has("tags") and has("tests"))'

expected_names=$(list_json | jq -r "$specs_jq | .title as \$t | .tests[] | \"[\" + .projectName + \"] \" + \$t" | sort)
expected_count=$(printf '%s\n' "$expected_names" | grep -c . )

# Orphan known issues: an entry whose screen, check kind or engine matches no test (a typo, or a removed screen or check)
# would never be looked at again, so it fails the stage. Judged against the full list (all engines, no name filter).
full_json=$(cd /verify/browser && env -u VERIFY_BROWSERS "$playwright_bin" test --list --reporter=json 2>/dev/null)
pairs=$(jq -r "$specs_jq | (.tags | map(ltrimstr(\"@\"))) as \$t | select((\$t | map(select(startswith(\"screen:\"))) | length) > 0) | (\$t | map(select(startswith(\"screen:\")))[0] | ltrimstr(\"screen:\")) + \"|\" + (\$t | map(select(startswith(\"kind:\")))[0] | ltrimstr(\"kind:\"))" <<<"$full_json" | sort -u)
engines=$(jq -r '.config.projects[].name' <<<"$full_json")
orphans=0
known_file=/verify/browser/known-issues.json
while IFS=$'\t' read -r k_screen k_kind k_id k_browsers; do
  [[ -z "$k_screen" ]] && continue
  if ! grep -Fxq "$k_screen|$k_kind" <<<"$pairs"; then
    stage_record FAIL "orphan known issue: $k_id" 0 "screen '$k_screen' and kind '$k_kind' match no test; fix the entry in verify/browser/known-issues.json or remove it"
    orphans=$((orphans + 1))
  fi
  for k_browser in ${k_browsers//,/ }; do
    if ! grep -Fxq "$k_browser" <<<"$engines"; then
      stage_record FAIL "orphan known issue: $k_id" 0 "browser '$k_browser' is not one of: $(tr '\n' ' ' <<<"$engines")"
      orphans=$((orphans + 1))
    fi
  done
done < <(jq -r '.[] | [.screen, .kind, .id, ((.browsers // []) | join(","))] | @tsv' "$known_file")
[[ "$orphans" -eq 0 ]] && stage_record PASS "known-issues entries each match a test" 0 "$(jq length "$known_file") entries"

(cd /verify/browser && VERIFY_BROWSER_RESULTS="$browser_jsonl" "$playwright_bin" test "${grep_args[@]}") > "$browser_out" 2>&1
pw_exit=$?
kill "$forward_pid" 2>/dev/null

seen=0
if [[ -s "$browser_jsonl" ]]; then
  while IFS= read -r line; do
    result=$(jq -r .result <<<"$line")
    name=$(jq -r .name <<<"$line")
    ms=$(jq -r .ms <<<"$line")
    detail=$(jq -r .detail <<<"$line")
    seen=$((seen + 1))
    stage_record "$result" "$name" "$ms" "$detail"
  done < <(jq -sc 'sort_by(.name)[]' "$browser_jsonl")
fi

if [[ "$seen" -eq 0 ]]; then
  stage_record FAIL "browser tests ran" 0 "exit code $pw_exit, no results; tail: $(tail -n 8 "$browser_out" | tr '\n' ' ')"
elif [[ "$seen" -ne "$expected_count" || "$expected_count" -eq 0 ]]; then
  # A crash or a dropped project must not pass: the number of results has to equal the number of listed tests.
  seen_names=$(jq -r .name "$browser_jsonl" | sort)
  missing=$(comm -23 <(printf '%s\n' "$expected_names") <(printf '%s\n' "$seen_names") | head -n 5 | tr '\n' ';')
  stage_record FAIL "browser results complete" 0 "expected $expected_count results (screens x checks x engines, from the test list) but got $seen; first missing: ${missing:-none}"
else
  stage_record PASS "browser results complete" 0 "$seen of $expected_count listed tests reported"
fi
if [[ "$pw_exit" -ne 0 && "$stage_fail" -eq 0 ]]; then
  stage_record FAIL "browser tests exited cleanly" 0 "exit code $pw_exit but no failing test was reported; tail: $(tail -n 8 "$browser_out" | tr '\n' ' ')"
fi

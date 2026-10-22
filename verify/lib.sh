#!/usr/bin/env bash
# Shared helpers for the verification run (TE10). Every check prints one line:
#   PASS|FAIL|SKIP|KNOWN  <stage>  <test name>  (<ms>)  [- detail]
# and appends one JSON object to $RESULTS_DIR/latest.jsonl. Stages are sourced by run.sh so the counters persist.

RESULTS_DIR=${RESULTS_DIR:-/results}
PASS=0
FAIL=0
SKIP=0
KNOWN=0   # a defect already listed in verify/browser/known-issues.json (TE5); counted apart from PASS and FAIL
STAGE=""

now_ms() { date +%s%3N; }
LAST_MS=$(now_ms)

record() { # result name ms [detail]
  local result="$1" name="$2" ms="$3" detail="${4:-}"
  printf '%-4s %-12s %s (%sms)%s\n' "$result" "$STAGE" "$name" "$ms" "${detail:+ - $detail}" | tee -a "$RESULTS_DIR/latest.log"
  jq -cn --arg r "$result" --arg s "$STAGE" --arg n "$name" --argjson ms "$ms" --arg d "$detail" \
    '{result: $r, stage: $s, test: $n, ms: $ms, detail: $d}' >> "$RESULTS_DIR/latest.jsonl"
  case "$result" in
    PASS) PASS=$((PASS + 1)) ;;
    FAIL) FAIL=$((FAIL + 1)) ;;
    SKIP) SKIP=$((SKIP + 1)) ;;
    KNOWN) KNOWN=$((KNOWN + 1)) ;;
  esac
}

# Records a result whose duration is not measured by the helper (unit-test stages report their own).
record_with_ms() { record "$@"; }

elapsed() { # milliseconds since the previous record, then resets the clock
  local end
  end=$(now_ms)
  ELAPSED=$((end - LAST_MS))
  LAST_MS=$end
}

check() { # name expected actual
  elapsed
  if [[ "$2" == "$3" ]]; then
    record PASS "$1" "$ELAPSED"
  else
    record FAIL "$1" "$ELAPSED" "expected '$2' but got '$3'"
  fi
}

check_contains() { # name needle haystack
  elapsed
  if [[ "$3" == *"$2"* ]]; then
    record PASS "$1" "$ELAPSED"
  else
    record FAIL "$1" "$ELAPSED" "expected to contain '$2'"
  fi
}

check_not_contains() { # name needle haystack
  elapsed
  if [[ "$3" != *"$2"* ]]; then
    record PASS "$1" "$ELAPSED"
  else
    record FAIL "$1" "$ELAPSED" "expected not to contain '$2'"
  fi
}

check_true() { # name description condition-exit-code
  elapsed
  if [[ "$3" == "0" ]]; then
    record PASS "$1" "$ELAPSED"
  else
    record FAIL "$1" "$ELAPSED" "$2"
  fi
}

skip() { # name reason
  elapsed
  record SKIP "$1" "$ELAPSED" "$2"
}

section() { printf '\n--- %s ---\n' "$1" | tee -a "$RESULTS_DIR/latest.log"; }

# --- HTTP helpers for the runtime stage: everything goes through Caddy over HTTPS, as a browser would. ---
BASE=https://localhost
curl_https() { curl -sk --resolve "localhost:443:${CADDY_IP}" --resolve "localhost:80:${CADDY_IP}" "$@"; }
code_of() { curl_https -o /dev/null -w '%{http_code}' "$@"; }

# Fetches a CSRF token into the cookie jar given as $1 and prints the token.
csrf_token() { curl_https -c "$1" -b "$1" "$BASE/api/auth/csrf" | jq -r .requestToken; }

# POST JSON with CSRF: post_json <jar> <path> <json> [extra curl args]; prints the status code, body in $BODY_FILE.
BODY_FILE=/tmp/body.json
post_json() {
  local jar="$1" path="$2" json="$3"
  shift 3
  local token
  token=$(csrf_token "$jar")
  curl_https -b "$jar" -c "$jar" -o "$BODY_FILE" -w '%{http_code}' -X POST \
    -H "X-CSRF-TOKEN: $token" -H 'Content-Type: application/json' -d "$json" "$@" "$BASE$path"
}

# Same with an arbitrary method (PUT, DELETE).
send_json() { # jar method path json
  local jar="$1" method="$2" path="$3" json="${4:-}" token
  token=$(csrf_token "$jar")
  curl_https -b "$jar" -c "$jar" -o "$BODY_FILE" -w '%{http_code}' -X "$method" \
    -H "X-CSRF-TOKEN: $token" -H 'Content-Type: application/json' ${json:+-d "$json"} "$BASE$path"
}

sign_in() { # jar email password -> status code
  post_json "$1" /api/auth/login "$(jq -cn --arg e "$2" --arg p "$3" '{email: $e, password: $p}')"
}

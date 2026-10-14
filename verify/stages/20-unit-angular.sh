#!/usr/bin/env bash
# Stage: the Angular unit tests (Karma + Jasmine in headless Chrome), one log line per spec.
STAGE=unit-angular
section "ng test"

out_file="$RESULTS_DIR/unit-angular.out"
(cd /src/src/GooseWebsite.Client && NO_COLOR=1 FORCE_COLOR=0 npm test -- --watch=false --karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox) > "$out_file" 2>&1
ng_exit=$?
# Karma colors its markers regardless of the environment; strip the escape codes before reading the log.
sed -i 's/\x1b\[[0-9;]*m//g' "$out_file"

seen=0
while IFS= read -r line; do
  # karma-spec-reporter prints "  ✓ name (0.002 secs)", "  ✗ name" and "  - name" for skipped specs.
  if [[ $line =~ ^[[:space:]]{2,}(✓|✗|-)[[:space:]]+(.+)$ ]]; then
    mark="${BASH_REMATCH[1]}"
    name="${BASH_REMATCH[2]}"
    ms=0
    if [[ $name =~ ^(.*)[[:space:]]\(([0-9.]+)[[:space:]]secs\)$ ]]; then
      name="${BASH_REMATCH[1]}"
      ms=$(awk -v s="${BASH_REMATCH[2]}" 'BEGIN { printf "%d", s * 1000 }')
    fi
    seen=$((seen + 1))
    case "$mark" in
      "✓") record PASS "$name" "$ms" ;;
      "✗") record FAIL "$name" "$ms" "see unit-angular.out" ;;
      *) record SKIP "$name" "$ms" ;;
    esac
  fi
done < "$out_file"

if [[ "$ng_exit" -ne 0 && "$FAIL" -eq 0 ]] || [[ "$seen" -eq 0 ]]; then
  record FAIL "ng test ran" 0 "exit code $ng_exit, $seen specs seen; tail: $(tail -n 5 "$out_file" | tr '\n' ' ')"
fi

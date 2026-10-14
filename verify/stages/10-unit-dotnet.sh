#!/usr/bin/env bash
# Stage: the .NET unit and integration tests (xUnit), one log line per test.
STAGE=unit-dotnet
section "dotnet test"

out_file="$RESULTS_DIR/unit-dotnet.out"
(cd /src && dotnet test GooseWebsite.slnx -p:SkipClientBuild=true --nologo --logger "console;verbosity=normal") > "$out_file" 2>&1
dotnet_exit=$?

seen=0
while IFS= read -r line; do
  if [[ $line =~ ^[[:space:]]+(Passed|Failed|Skipped)[[:space:]]+(.+)[[:space:]]\[([^]]*)\]$ ]]; then
    seen=$((seen + 1))
    outcome="${BASH_REMATCH[1]}"
    name="${BASH_REMATCH[2]}"
    timing="${BASH_REMATCH[3]}"
    ms=0
    if [[ $timing =~ ([0-9]+)[[:space:]]ms ]]; then ms="${BASH_REMATCH[1]}"; fi
    if [[ $timing =~ ([0-9.]+)[[:space:]]s$ ]]; then ms=$(awk -v s="${BASH_REMATCH[1]}" 'BEGIN { printf "%d", s * 1000 }'); fi
    case "$outcome" in
      Passed) record PASS "$name" "$ms" ;;
      Failed) record FAIL "$name" "$ms" "see unit-dotnet.out" ;;
      Skipped) record SKIP "$name" "$ms" ;;
    esac
  fi
done < "$out_file"

# A build error or a crashed runner prints no per-test lines; never let that look like success.
if [[ "$dotnet_exit" -ne 0 && "$FAIL" -eq 0 ]] || [[ "$seen" -eq 0 ]]; then
  record FAIL "dotnet test ran" 0 "exit code $dotnet_exit, $seen tests seen; tail: $(tail -n 5 "$out_file" | tr '\n' ' ')"
fi

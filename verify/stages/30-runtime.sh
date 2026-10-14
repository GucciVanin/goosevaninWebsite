#!/usr/bin/env bash
# Stage: runtime tests against the running stack, one file per feature in /verify/runtime. They are sourced in
# name order and share state (the counters and the administrator's cookie jar). Add a file per new feature.
STAGE=runtime
LAST_MS=$(now_ms)
section "runtime tests against https://localhost (through Caddy)"

JAR_DIR=$(mktemp -d)
ADMIN_JAR="$JAR_DIR/admin.jar"

for runtime_script in /verify/runtime/*.sh; do
  section "$(basename "$runtime_script" .sh)"
  # shellcheck disable=SC1090
  source "$runtime_script"
done

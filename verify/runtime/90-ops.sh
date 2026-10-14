#!/usr/bin/env bash
# Feature: operator commands (admin provision, admin reset-password) and the startup migrations. The host script
# runs the commands before this stage and leaves their exit codes in ops.env and the web log in web.log.
if [[ -f "$RESULTS_DIR/ops.env" ]]; then
  # shellcheck disable=SC1091
  source <(tr -d '\r' < "$RESULTS_DIR/ops.env")
fi

check "admin provision created the first administrator (exit 0)" 0 "${OPS_PROVISION_EXIT:-missing}"
check "admin provision created the second administrator (exit 0)" 0 "${OPS_PROVISION2_EXIT:-missing}"
check "admin provision refuses an email that already exists (exit 1)" 1 "${OPS_DUPLICATE_PROVISION_EXIT:-missing}"
check "admin reset-password changed an administrator's password (exit 0)" 0 "${OPS_RESET_EXIT:-missing}"
check "admin reset-password refuses an unknown email (exit 1)" 1 "${OPS_UNKNOWN_RESET_EXIT:-missing}"

ops_log=$(cat "$RESULTS_DIR/ops.log" 2>/dev/null)
check_contains "the refusal to provision a duplicate says the account already exists" "already exists" "$ops_log"
check_contains "the refusal to reset an unknown email says no administrator exists" "No administrator account exists for that email" "$ops_log"

jar="$JAR_DIR/admin2.jar"
check "the old password no longer works after a reset" 401 "$(sign_in "$jar" "$VERIFY_ADMIN2_EMAIL" "$VERIFY_ADMIN2_PASSWORD")"
check "the new password works after a reset" 200 "$(sign_in "$jar" "$VERIFY_ADMIN2_EMAIL" "$VERIFY_ADMIN2_NEW_PASSWORD")"
check "another administrator is unaffected by the reset" 200 "$(sign_in "$JAR_DIR/admin1-again.jar" "$VERIFY_ADMIN_EMAIL" "$VERIFY_ADMIN_PASSWORD")"

# Startup log of the web container: the schema was built by migrations and nothing failed.
if [[ -f "$RESULTS_DIR/web.log" ]]; then
  web_log=$(cat "$RESULTS_DIR/web.log")
  check_contains "the schema was created by the InitialCreate migration" "InitialCreate" "$web_log"
  check_not_contains "the web container logged no failures at startup" $'\nfail:' $'\n'"$web_log"
else
  skip "startup log checks" "web.log was not captured"
fi

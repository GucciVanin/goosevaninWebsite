#!/usr/bin/env bash
# Feature: administrator authentication (cookie session, CSRF, lockout-safe checks, sign-out).
[[ -n "${VERIFY_ADMIN_EMAIL:-}" && -n "${VERIFY_ADMIN_PASSWORD:-}" ]]
check_true "the test administrator settings are present" "VERIFY_ADMIN_EMAIL / VERIFY_ADMIN_PASSWORD are missing (see .env.verify.example)" $?
anon_jar="$JAR_DIR/anon.jar"

csrf_headers=$(curl_https -D - -o /dev/null -c "$anon_jar" "$BASE/api/auth/csrf" | tr -d '\r')
csrf_cookie=$(grep -i '^set-cookie: goosewebsite.csrf=' <<<"$csrf_headers" | tr 'A-Z' 'a-z')
check "the CSRF endpoint answers 200" 200 "$(code_of "$BASE/api/auth/csrf")"
check_contains "the CSRF cookie is Secure" "secure" "$csrf_cookie"
check_contains "the CSRF cookie is HttpOnly" "httponly" "$csrf_cookie"
check_contains "the CSRF cookie is SameSite=Lax" "samesite=lax" "$csrf_cookie"

check "sign-in without a CSRF token is rejected" 400 "$(code_of -X POST -H 'Content-Type: application/json' -d '{"email":"a@example.test","password":"x"}' "$BASE/api/auth/login")"
check "sign-in with an unknown email is refused" 401 "$(sign_in "$anon_jar" nobody@example.test 'Wrong-password-123')"
unknown_body=$(cat "$BODY_FILE")
check "sign-in with a wrong password is refused" 401 "$(sign_in "$anon_jar" "$VERIFY_ADMIN_EMAIL" 'Wrong-password-123')"
check "a wrong password and an unknown email get the same answer" "$unknown_body" "$(cat "$BODY_FILE")"
check "the protected profile endpoint refuses an anonymous caller" 401 "$(code_of "$BASE/api/auth/me")"

# Valid sign-in, capturing the session cookie's flags.
token=$(csrf_token "$ADMIN_JAR")
login_code=$(curl_https -b "$ADMIN_JAR" -c "$ADMIN_JAR" -D "$JAR_DIR/login.headers" -o "$BODY_FILE" -w '%{http_code}' -X POST \
  -H "X-CSRF-TOKEN: $token" -H 'Content-Type: application/json' \
  -d "$(jq -cn --arg e "$VERIFY_ADMIN_EMAIL" --arg p "$VERIFY_ADMIN_PASSWORD" '{email: $e, password: $p}')" "$BASE/api/auth/login")
auth_cookie=$(grep -i '^set-cookie: goosewebsite.auth=' "$JAR_DIR/login.headers" | tr -d '\r' | tr 'A-Z' 'a-z')
check "the seeded administrator can sign in" 200 "$login_code"
check_contains "the session cookie is Secure" "secure" "$auth_cookie"
check_contains "the session cookie is HttpOnly" "httponly" "$auth_cookie"
check_contains "the session cookie is SameSite=Lax" "samesite=lax" "$auth_cookie"
check_not_contains "the session cookie is a session cookie (no fixed expiry)" "expires=" "$auth_cookie"

me=$(curl_https -b "$ADMIN_JAR" "$BASE/api/auth/me")
check "the signed-in profile says administrator" true "$(jq -r .isAdmin <<<"$me")"
check "the signed-in profile shows the display name" "Site administrator" "$(jq -r .displayName <<<"$me")"
check "the signed-in profile never includes the email" false "$(jq 'has("email")' <<<"$me")"

cp "$ADMIN_JAR" "$JAR_DIR/admin-before-logout.jar"
check "sign-out answers 204" 204 "$(send_json "$ADMIN_JAR" POST /api/auth/logout)"
check "the profile endpoint refuses the signed-out client" 401 "$(code_of -b "$ADMIN_JAR" "$BASE/api/auth/me")"
check "a signed-out session cookie replayed later is rejected" 401 "$(code_of -b "$JAR_DIR/admin-before-logout.jar" "$BASE/api/auth/me")"

# Sign in again for the features that need an administrator.
check "the administrator can sign in again" 200 "$(sign_in "$ADMIN_JAR" "$VERIFY_ADMIN_EMAIL" "$VERIFY_ADMIN_PASSWORD")"

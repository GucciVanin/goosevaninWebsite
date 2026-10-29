#!/usr/bin/env bash
# Feature: reader registration. Its on/off state must match the endpoint's behavior. When it is on, the test does
# not register anyone: that would send a real verification email. The unit tests cover the enabled flow.
reader_jar="$JAR_DIR/reader.jar"
enabled=$(curl_https "$BASE/api/auth/reader-registration" | jq -r .enabled)
check "the registration state endpoint answers 200" 200 "$(code_of "$BASE/api/auth/reader-registration")"
check "the registration state is reported as true or false" true "$([[ "$enabled" == true || "$enabled" == false ]] && echo true || echo false)"

register_body='{"email":"verify-reader@example.test","password":"Verify-reader-pass-1","displayName":"Verify Reader"}'
if [[ "$enabled" == "false" ]]; then
  check "registration is refused with 404 while it is switched off" 404 "$(post_json "$reader_jar" /api/auth/register "$register_body")"
  # F3-US1-AC4: a body that asks for a role or administrator rights is answered as before (404 while registration is
  # off) and creates nothing: that email cannot sign in. (With registration on the same request would send a
  # verification email, so the role-ignored behaviour with registration on stays in xUnit:
  # AuthApiIntegrationTests.PublicRegistrationIgnoresRequestedAdminRoleAndCreatesReaderAccount.)
  role_probe_password='Verify-role-probe-pass-1'
  role_body=$(jq -cn --arg p "$role_probe_password" '{email: "verify-role-probe@example.test", password: $p, displayName: "Verify Role Probe", role: "Admin", isAdmin: true}')
  check "a registration body carrying role and isAdmin is answered as before (404 while registration is off)" 404 "$(post_json "$reader_jar" /api/auth/register "$role_body")"
  check "no account that can sign in results from a registration body carrying role and isAdmin" 401 "$(sign_in "$reader_jar" verify-role-probe@example.test "$role_probe_password")"
  # The rate limit applies before the endpoint runs: five requests per window per client.
  last=0
  for _ in 1 2 3 4 5; do last=$(post_json "$reader_jar" /api/auth/register "$register_body"); done
  check "registration requests are rate limited after five tries" 429 "$last"
else
  skip "registration is refused while switched off" "registration is switched on in this stack"
fi

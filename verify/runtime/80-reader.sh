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
  # The rate limit applies before the endpoint runs: five requests per window per client.
  last=0
  for _ in 1 2 3 4 5; do last=$(post_json "$reader_jar" /api/auth/register "$register_body"); done
  check "registration requests are rate limited after five tries" 429 "$last"
else
  skip "registration is refused while switched off" "registration is switched on in this stack"
fi

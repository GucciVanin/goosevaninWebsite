#!/usr/bin/env bash
# Feature: subscriptions (F10-US1). Only requests that send no email are made: the stack may have a real mail provider
# configured (.env), and a valid subscription emails a confirmation link to the address given. So this file checks the
# refusals and the shape of the answers; the double opt-in itself (pending record, one mail, identical answers for new /
# pending / confirmed addresses, expiry, reuse, the stored columns, log privacy) is covered by the xUnit tests in
# SubscriptionsApiTests, which use the test mail service. At most four requests reach the subscribe endpoint per run
# (the limit is five per address per 10 minutes, and the stack restarts at the start of every run).
json_header='Content-Type: application/json'
sub_jar="$JAR_DIR/subscriptions.jar"
subscribe() { post_json "$sub_jar" /api/subscriptions "$1"; }

check "a subscription without a CSRF token is rejected" 400 \
  "$(code_of -X POST -H "$json_header" -d '{"name":"V","email":"v@example.test","website":""}' "$BASE/api/subscriptions")"

check "a filled honeypot is answered like any other submission" 200 "$(subscribe '{"name":"V","email":"honeypot@example.test","website":"http://spam.example"}')"
honeypot_body=$(cat "$BODY_FILE")
check "an invalid email address gets the same status" 200 "$(subscribe '{"name":"V","email":"not-an-email","website":""}')"
check "an invalid email address gets the same answer as the honeypot" "$honeypot_body" "$(cat "$BODY_FILE")"
check_not_contains "the answer never echoes the address" "honeypot@example.test" "$honeypot_body"
check_contains "the answer says a confirmation is on its way, not that the address was added" "confirmation email is on its way" "$honeypot_body"

check "an unknown confirmation token is refused with 400" 400 "$(post_json "$sub_jar" /api/subscriptions/confirm '{"token":"not-a-real-token"}')"
check "the refusal says the link is no longer valid" "This confirmation link is no longer valid." "$(jq -r .error "$BODY_FILE")"
check "an empty confirmation token is refused with the same status" 400 "$(post_json "$sub_jar" /api/subscriptions/confirm '{"token":""}')"
check "a confirmation without a CSRF token is rejected" 400 \
  "$(code_of -X POST -H "$json_header" -d '{"token":"x"}' "$BASE/api/subscriptions/confirm")"
check_contains "a confirmation link opened with GET only serves the app, it never confirms" "<app-root" "$(curl_https "$BASE/api/subscriptions/confirm?token=x")"
check_not_contains "no endpoint lists subscribers" "subscriber" "$(curl_https "$BASE/api/subscriptions")"

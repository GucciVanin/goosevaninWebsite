#!/usr/bin/env bash
# Feature: contact form. Only requests that are refused are sent: a valid submission would email a real address.
json_header='Content-Type: application/json'
contact() { code_of -X POST -H "$json_header" -d "$1" "$BASE/api/contact"; }

check "a filled honeypot is rejected" 400 "$(contact '{"name":"V","email":"v@example.test","reason":"Personal note","message":"m","website":"spam"}')"
check "missing fields are rejected" 400 "$(contact '{"name":"V","email":"","reason":"Personal note","message":"m"}')"
check "an unknown reason is rejected" 400 "$(contact '{"name":"V","email":"v@example.test","reason":"Nope","message":"m"}')"
check "an invalid email address is rejected" 400 "$(contact '{"name":"V","email":"not-an-email","reason":"Personal note","message":"m"}')"
check "an over-long message is rejected" 400 "$(contact "$(jq -cn --arg m "$(head -c 2100 /dev/zero | tr '\0' 'x')" '{name: "V", email: "v@example.test", reason: "Personal note", message: $m}')")"

check "an unknown verification token is refused with 409" 409 "$(code_of -X POST -H "$json_header" -d '{"token":"not-a-real-token"}' "$BASE/api/contact/verify")"
check "an empty verification token is rejected" 400 "$(code_of -X POST -H "$json_header" -d '{"token":""}' "$BASE/api/contact/verify")"
check_contains "a verification link opened with GET only serves the app, it never delivers" "<app-root" "$(curl_https "$BASE/api/contact/verify?token=x")"

# The reasons and honeypot answer with the documented generic messages, never internals.
body=$(curl_https -X POST -H "$json_header" -d '{"name":"V","email":"v@example.test","reason":"Nope","message":"m"}' "$BASE/api/contact")
check "the refusal explains the reason in plain words" "Please choose a valid reason." "$(jq -r .error <<<"$body")"

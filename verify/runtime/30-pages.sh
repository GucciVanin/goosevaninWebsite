#!/usr/bin/env bash
# Feature: public profile site (the Angular app). Only the served shell is checked here; the rendered content is
# covered by the Angular unit tests, and browser-level checks belong to enabler TE5.
html=$(curl_https "$BASE/")
check "GET / answers 200" 200 "$(code_of "$BASE/")"
check_contains "the page is the Angular shell" "<app-root" "$html"
check_contains "the page title names Gustavo" "<title>Gustavo Couto Vanin</title>" "$html"

bundle=$(grep -o 'main-[A-Za-z0-9_-]*\.js' <<<"$html" | head -n 1)
check_true "the shell references the main script bundle" "no main-*.js reference found" "$([[ -n "$bundle" ]]; echo $?)"
check "the main script bundle is served" 200 "$(code_of "$BASE/$bundle")"
check "the favicon is served" 200 "$(code_of "$BASE/favicon.ico")"

for route in /sign-in /verify-email /contact/verify /editor /access-denied /a-route-that-does-not-exist; do
  check "route $route serves the app" 200 "$(code_of "$BASE$route")"
done

#!/usr/bin/env bash
# Feature: health. The container is healthy only when the application and its database answer.
body=$(curl_https "$BASE/api/health")
check "GET /api/health answers 200" 200 "$(code_of "$BASE/api/health")"
check "health status is ok" ok "$(jq -r .status <<<"$body")"
check "health database is ok" ok "$(jq -r .database <<<"$body")"
started=$(jq -r .startedAtUtc <<<"$body")
[[ -n "$started" && "$started" != null ]]
check_true "health reports the process start time" "startedAtUtc is missing" $?
check "health sets no cookie" "" "$(curl_https -D - -o /dev/null "$BASE/api/health" | tr -d '\r' | grep -i '^set-cookie')"

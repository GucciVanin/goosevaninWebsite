#!/usr/bin/env bash
# Feature: contact email provider (MailerSend). The provider is external, so only its authorization is checked,
# read-only: no email is sent and the API key is never printed. 200 means authorized; 403 means the key is
# valid but not scoped for this read endpoint; 401 means the key is rejected.
if [[ -z "${MAIL_KEY:-}" ]]; then
  skip "mail provider authorizes the container's API key" "MAILERSEND_API_KEY is not configured in .env"
else
  status=$(printf 'header = "Authorization: Bearer %s"\n' "$MAIL_KEY" \
    | curl -s -o /dev/null -w '%{http_code}' --max-time 15 -K - https://api.mailersend.com/v1/api-quota)
  case "$status" in
    200) elapsed; record PASS "mail provider authorizes the container's API key" "$ELAPSED" ;;
    403) elapsed; record PASS "mail provider authorizes the container's API key" "$ELAPSED" "key is valid; this read endpoint is outside its scope" ;;
    *) elapsed; record FAIL "mail provider authorizes the container's API key" "$ELAPSED" "provider answered HTTP $status (000 means unreachable, 401 means the key is rejected)" ;;
  esac
fi

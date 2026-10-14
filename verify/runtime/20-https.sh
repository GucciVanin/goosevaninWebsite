#!/usr/bin/env bash
# Feature: HTTPS only. Plain HTTP redirects, browsers are told to stay on HTTPS, the certificate is served.
http_headers=$(curl_https -D - -o /dev/null http://localhost/ | tr -d '\r')
check "HTTP redirects with 308" 308 "$(code_of http://localhost/)"
check_contains "HTTP redirect points to https" "https://localhost/" "$(grep -i '^location:' <<<"$http_headers")"

headers=$(curl_https -D - -o /dev/null "$BASE/" | tr -d '\r')
check_contains "HSTS header is sent" "max-age=" "$(grep -i '^strict-transport-security:' <<<"$headers")"
check_contains "Referrer-Policy is no-referrer" "no-referrer" "$(grep -i '^referrer-policy:' <<<"$headers")"

issuer=$(curl -skv --resolve "localhost:443:${CADDY_IP}" "$BASE/" 2>&1 | grep -i 'issuer:' | head -n 1)
check_contains "certificate is issued by the Caddy local authority" "Caddy Local Authority" "$issuer"

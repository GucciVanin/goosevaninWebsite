#!/usr/bin/env bash
# Feature: blog posts. Public reads show published posts only; only an administrator with a CSRF token can write.
suffix=$(date +%s)
post_body() { jq -cn --arg t "$1" --argjson p "$2" \
  '{title: $t, excerpt: "Created by the verification run.", content: "body", category: "Testing", tags: ["verify"], isPublished: $p}'; }

# Writes are refused to anonymous callers and to administrators without a CSRF token.
check "an anonymous caller cannot create a post" 401 "$(post_json "$JAR_DIR/anon.jar" /api/blog "$(post_body 'Anonymous attempt' true)")"
check "an administrator without a CSRF token cannot create a post" 400 \
  "$(code_of -b "$ADMIN_JAR" -X POST -H 'Content-Type: application/json' -d "$(post_body 'No CSRF' true)" "$BASE/api/blog")"

# Published post: create, read, list, update, delete.
check "an administrator can create a published post" 201 "$(post_json "$ADMIN_JAR" /api/blog "$(post_body "Verify published $suffix" true)")"
published_id=$(jq -r .id "$BODY_FILE")
published_slug=$(jq -r .slug "$BODY_FILE")
check "the new post is readable by its slug" 200 "$(code_of "$BASE/api/blog/$published_slug")"
check_contains "the new post appears in the public list" "$published_slug" "$(curl_https "$BASE/api/blog" | jq -r '.[].slug')"
check "an administrator can update the post" 200 "$(send_json "$ADMIN_JAR" PUT "/api/blog/$published_id" "$(post_body "Verify updated $suffix" true)")"
check "the update is visible to the public" "Verify updated $suffix" "$(curl_https "$BASE/api/blog/$published_slug" | jq -r .title)"

# Draft post: never visible to the public.
check "an administrator can create a draft" 201 "$(post_json "$ADMIN_JAR" /api/blog "$(post_body "Verify draft $suffix" false)")"
draft_id=$(jq -r .id "$BODY_FILE")
draft_slug=$(jq -r .slug "$BODY_FILE")
check "a draft is not readable by the public" 404 "$(code_of "$BASE/api/blog/$draft_slug")"
check_not_contains "a draft is not in the public list" "$draft_slug" "$(curl_https "$BASE/api/blog" | jq -r '.[].slug')"

# Clean up what this run created, and prove deletion works.
check "an administrator can delete the draft" 204 "$(send_json "$ADMIN_JAR" DELETE "/api/blog/$draft_id")"
check "an administrator can delete the published post" 204 "$(send_json "$ADMIN_JAR" DELETE "/api/blog/$published_id")"
check "a deleted post is gone" 404 "$(code_of "$BASE/api/blog/$published_slug")"
check "deleting a post that does not exist is a 404" 404 "$(send_json "$ADMIN_JAR" DELETE "/api/blog/$published_id")"

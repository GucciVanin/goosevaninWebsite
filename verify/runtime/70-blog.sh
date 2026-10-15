#!/usr/bin/env bash
# Feature: stories (the blog module). Public reads show published stories only. Only an administrator with a CSRF
# token can write, saving never publishes, and a draft is never public. The editor API is /api/editor/stories.
suffix=$(date +%s)
EDITOR_API=/api/editor/stories
story_body() { jq -cn --arg t "$1" --arg e "$2" --arg c "$3" \
  '{title: $t, excerpt: $e, content: $c, category: "Testing", tags: ["verify"]}'; }

# Writes are refused to anonymous callers and to administrators without a CSRF token.
check "an anonymous caller cannot create a story" 401 "$(post_json "$JAR_DIR/anon.jar" $EDITOR_API "$(story_body 'Anonymous attempt' e c)")"
check "an anonymous caller cannot update a story" 401 "$(send_json "$JAR_DIR/anon.jar" PUT $EDITOR_API/1 "$(story_body 'Anonymous update' e c)")"
check "an anonymous caller cannot delete a story" 401 "$(send_json "$JAR_DIR/anon.jar" DELETE $EDITOR_API/1)"
check "an anonymous caller cannot publish a story" 401 "$(send_json "$JAR_DIR/anon.jar" POST $EDITOR_API/1/publish)"
check "an anonymous caller cannot read the editor list" 401 "$(code_of "$BASE$EDITOR_API")"
check "an administrator without a CSRF token cannot create a story" 400 \
  "$(code_of -b "$ADMIN_JAR" -X POST -H 'Content-Type: application/json' -d "$(story_body 'No CSRF' e c)" "$BASE$EDITOR_API")"

# A new story is a draft, whatever the request says; it is not public.
check "an administrator can create a draft" 201 "$(post_json "$ADMIN_JAR" $EDITOR_API "$(story_body "Verify story $suffix" 'A short excerpt.' $'# Heading\n\nSome *text*.')")"
story_id=$(jq -r .id "$BODY_FILE")
story_slug=$(jq -r .slug "$BODY_FILE")
check "a new story is a draft" false "$(jq -r .isPublished "$BODY_FILE")"
check "the address is made from the title" "verify-story-$suffix" "$story_slug"
check "a draft is not readable by the public" 404 "$(code_of "$BASE/api/blog/$story_slug")"
check_not_contains "a draft is not in the public list" "$story_slug" "$(curl_https "$BASE/api/blog" | jq -r '.[].slug')"
check_contains "the editor list shows the draft" "$story_slug" "$(curl_https -b "$ADMIN_JAR" "$BASE$EDITOR_API" | jq -r '.[].slug')"
check "the editor can open the draft to preview it" 200 "$(code_of -b "$ADMIN_JAR" "$BASE$EDITOR_API/$story_id")"

# Saving edits does not publish and keeps the address.
check "an administrator can save edits" 200 "$(send_json "$ADMIN_JAR" PUT $EDITOR_API/$story_id "$(story_body "Verify story renamed $suffix" 'A changed excerpt.' 'New body.')")"
check "saving does not publish" false "$(jq -r .isPublished "$BODY_FILE")"
check "saving keeps the address" "$story_slug" "$(jq -r .slug "$BODY_FILE")"

# Publishing needs an excerpt and a body, happens once, and makes the story public.
incomplete_id=$(post_json "$ADMIN_JAR" $EDITOR_API "$(story_body "Verify incomplete $suffix" '' '')" >/dev/null; jq -r .id "$BODY_FILE")
check "a draft without an excerpt and text cannot be published" 400 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$incomplete_id/publish)"
check "an administrator can publish a complete draft" 200 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$story_id/publish)"
published_at=$(jq -r .publishedAtUtc "$BODY_FILE")
check "publishing sets the story to published" true "$(jq -r .isPublished "$BODY_FILE")"
check "the published story is readable by the public" 200 "$(code_of "$BASE/api/blog/$story_slug")"
check_contains "the published story is in the public list" "$story_slug" "$(curl_https "$BASE/api/blog" | jq -r '.[].slug')"
send_json "$ADMIN_JAR" POST $EDITOR_API/$story_id/publish >/dev/null
check "publishing again keeps the same publication date" "$published_at" "$(jq -r .publishedAtUtc "$BODY_FILE")"

# Back to draft hides it again.
check "an administrator can return a story to draft" 200 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$story_id/unpublish)"
check "a story returned to draft is not public" 404 "$(code_of "$BASE/api/blog/$story_slug")"

# Clean up what this run created, and prove deletion works.
check "an administrator can delete a story" 204 "$(send_json "$ADMIN_JAR" DELETE $EDITOR_API/$story_id)"
check "the incomplete draft can be deleted too" 204 "$(send_json "$ADMIN_JAR" DELETE $EDITOR_API/$incomplete_id)"
check "a deleted story is gone" 404 "$(code_of -b "$ADMIN_JAR" "$BASE$EDITOR_API/$story_id")"
check "deleting a story that does not exist is a 404" 404 "$(send_json "$ADMIN_JAR" DELETE $EDITOR_API/$story_id)"

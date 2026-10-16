#!/usr/bin/env bash
# Feature: projects. Public reads show published projects only. Only an administrator with a CSRF token can write,
# saving never publishes, links must be web addresses, and a draft is never public. The editor API is /api/editor/projects.
suffix=$(date +%s)
EDITOR_API=/api/editor/projects
project_body() { jq -cn --arg t "$1" --arg o "$2" --arg d "$3" \
  '{title: $t, overview: $o, contribution: "Everything.", technologies: ["C#", "Angular"], demoUrl: $d, sourceUrl: "https://github.com/example/site"}'; }

check "an anonymous caller cannot create a project" 401 "$(post_json "$JAR_DIR/anon.jar" $EDITOR_API "$(project_body 'Anonymous attempt' o https://example.test)")"
check "an anonymous caller cannot publish a project" 401 "$(send_json "$JAR_DIR/anon.jar" POST $EDITOR_API/1/publish)"
check "an anonymous caller cannot read the editor list" 401 "$(code_of "$BASE$EDITOR_API")"
check "an administrator without a CSRF token cannot create a project" 400 \
  "$(code_of -b "$ADMIN_JAR" -X POST -H 'Content-Type: application/json' -d "$(project_body 'No CSRF' o https://example.test)" "$BASE$EDITOR_API")"

check "an administrator can create a draft project" 201 "$(post_json "$ADMIN_JAR" $EDITOR_API "$(project_body "Verify project $suffix" $'# About\n\nA *test*.' https://example.test/demo)")"
project_id=$(jq -r .id "$BODY_FILE")
project_slug=$(jq -r .slug "$BODY_FILE")
check "a new project is a draft" false "$(jq -r .isPublished "$BODY_FILE")"
check "the address is made from the title" "verify-project-$suffix" "$project_slug"
check "a draft project is not readable by the public" 404 "$(code_of "$BASE/api/projects/$project_slug")"
check_not_contains "a draft project is not in the public list" "$project_slug" "$(curl_https "$BASE/api/projects" | jq -r '.[].slug')"
check "the editor can open the draft to preview it" 200 "$(code_of -b "$ADMIN_JAR" "$BASE$EDITOR_API/$project_id")"

check "a link that is not a web address is refused" 400 "$(post_json "$ADMIN_JAR" $EDITOR_API "$(project_body "Bad link $suffix" o 'javascript:alert(1)')")"
check "saving edits does not publish" false "$(send_json "$ADMIN_JAR" PUT $EDITOR_API/$project_id "$(project_body "Verify project renamed $suffix" 'Changed.' https://example.test/demo)" >/dev/null; jq -r .isPublished "$BODY_FILE")"

bare_id=$(post_json "$ADMIN_JAR" $EDITOR_API '{"title":"Verify bare '"$suffix"'"}' >/dev/null; jq -r .id "$BODY_FILE")
check "a project without an overview cannot be published" 400 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$bare_id/publish)"
check "an administrator can publish a complete project" 200 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$project_id/publish)"
published_at=$(jq -r .publishedAtUtc "$BODY_FILE")
check "the published project is readable by the public" 200 "$(code_of "$BASE/api/projects/$project_slug")"
send_json "$ADMIN_JAR" POST $EDITOR_API/$project_id/publish >/dev/null
check "publishing again keeps the same publication date" "$published_at" "$(jq -r .publishedAtUtc "$BODY_FILE")"
check "an administrator can return a project to draft" 200 "$(send_json "$ADMIN_JAR" POST $EDITOR_API/$project_id/unpublish)"
check "a project returned to draft is not public" 404 "$(code_of "$BASE/api/projects/$project_slug")"

check "an administrator can delete a project" 204 "$(send_json "$ADMIN_JAR" DELETE $EDITOR_API/$project_id)"
check "the bare draft can be deleted too" 204 "$(send_json "$ADMIN_JAR" DELETE $EDITOR_API/$bare_id)"
check "a deleted project is gone" 404 "$(code_of -b "$ADMIN_JAR" "$BASE$EDITOR_API/$project_id")"

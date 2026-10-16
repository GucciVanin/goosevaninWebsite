#!/usr/bin/env bash
# Feature: image uploads (Media module). Only an administrator with a CSRF token can upload; only real JPEG, PNG and
# WebP files up to 5 MB are stored, under generated names; a refused file is never stored or publicly addressable.
UPLOAD_API=/api/editor/images
workdir=$(mktemp -d)
printf '\x89PNG\r\n\x1a\n\0\0\0\rIHDR\0\0\0\x01\0\0\0\x01\x08\x06\0\0\0' > "$workdir/ok.png"
printf 'this is a script, not an image' > "$workdir/fake.png"
head -c $((5 * 1024 * 1024 + 1)) /dev/zero > "$workdir/big.png"; printf '\x89PNG\r\n\x1a\n' | dd of="$workdir/big.png" conv=notrunc 2>/dev/null

upload() { # jar file [no-token]
  local jar="$1" file="$2" token=""
  [ -z "${3:-}" ] && token=$(csrf_token "$jar")
  curl_https -b "$jar" -c "$jar" -o "$BODY_FILE" -w '%{http_code}' -X POST ${token:+-H "X-CSRF-TOKEN: $token"} \
    -F "file=@$file;type=image/png" "$BASE$UPLOAD_API"
}

check "an anonymous caller cannot upload an image" 401 "$(upload "$JAR_DIR/anon.jar" "$workdir/ok.png")"
check "an administrator without a CSRF token cannot upload an image" 400 "$(upload "$ADMIN_JAR" "$workdir/ok.png" no-token)"
check "a file that is not an image is refused" 400 "$(upload "$ADMIN_JAR" "$workdir/fake.png")"
check "an image over 5 MB is refused" 413 "$(upload "$ADMIN_JAR" "$workdir/big.png")"

check "an administrator can upload a PNG" 201 "$(upload "$ADMIN_JAR" "$workdir/ok.png")"
image_id=$(jq -r .id "$BODY_FILE")
image_url=$(jq -r .url "$BODY_FILE")
check_contains "the stored name is generated, not the uploaded name" "/uploads/" "$image_url"
check "the uploaded image is publicly served" 200 "$(code_of "$BASE$image_url")"
check "the served image has its real type" "image/png" "$(curl_https -o /dev/null -w '%{content_type}' "$BASE$image_url")"
check "browsers are told not to guess the type" "nosniff" "$(curl_https -sI "$BASE$image_url" | tr -d '\r' | awk -F': ' 'tolower($1)=="x-content-type-options"{print $2}')"
check "a path that is not an uploaded image is not served" 404 "$(code_of "$BASE/uploads/fake.png")"

check "an administrator can delete the image" 204 "$(send_json "$ADMIN_JAR" DELETE $UPLOAD_API/$image_id)"
check "a deleted image is no longer served" 404 "$(code_of "$BASE$image_url")"
rm -rf "$workdir"

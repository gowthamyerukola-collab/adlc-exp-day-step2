#!/bin/sh
set -eu

HTML_INDEX="/usr/share/nginx/html/index.html"
TOKEN="__VITE_API_URL__"
VALUE="${VITE_API_URL:-}"

# Replace runtime placeholder in the built HTML.
if [ -f "$HTML_INDEX" ]; then
  # Escape forward slashes for sed.
  ESCAPED_VALUE=$(printf '%s' "$VALUE" | sed 's/[\/&]/\\&/g')
  sed -i "s|$TOKEN|$ESCAPED_VALUE|g" "$HTML_INDEX"
fi

exec nginx -g 'daemon off;'

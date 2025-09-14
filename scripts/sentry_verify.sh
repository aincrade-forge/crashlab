#!/usr/bin/env bash
set -euo pipefail

# Verifies Sentry ingestion by querying recent events filtered by run_id.
# Requires: sentry-cli, SENTRY_ORG, SENTRY_PROJECT, SENTRY_AUTH_TOKEN, RUN_ID

: "${SENTRY_ORG:?Set SENTRY_ORG}"
: "${SENTRY_PROJECT:?Set SENTRY_PROJECT}"
: "${SENTRY_AUTH_TOKEN:?Set SENTRY_AUTH_TOKEN}"
: "${RUN_ID:?Set RUN_ID (e.g., demo-run)}"

QUERY="run_id:${RUN_ID}"
LIMIT=${LIMIT:-10}

echo "Querying Sentry events for: $QUERY"
sentry-cli api \
  "/api/0/projects/${SENTRY_ORG}/${SENTRY_PROJECT}/events/?query=$(printf %s "$QUERY" | jq -sRr @uri)&per_page=${LIMIT}" \
  | jq '.[] | {id, eventID, title, timestamp, tags: [.tags[] | select(.key=="run_id" or .key=="backend" or .key=="platform" or .key=="commit_sha" or .key=="build_number") ] }'

echo "Done."


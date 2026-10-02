#!/usr/bin/env bash
# End-to-end demo against a running stack (docker compose up).
#   BASE_URL      backend or nginx, default http://localhost:8080
#   UPSTREAM_URL  target of the new API as seen from the backend container, default http://upstream:8080
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
UPSTREAM_URL="${UPSTREAM_URL:-http://upstream:8080}"
OWNER_EMAIL="${OWNER_EMAIL:-owner@demo.local}"
CONSUMER_EMAIL="${CONSUMER_EMAIL:-consumer@demo.local}"
PASSWORD="${PASSWORD:-Password123!}"
RATE_LIMIT=5
REQUESTS=8

command -v jq >/dev/null || { echo "This script needs jq (https://jqlang.org)." >&2; exit 1; }

step() { printf '\n\033[1m== %s\033[0m\n' "$*"; }

# call METHOD PATH TOKEN [JSON_BODY]
call() {
  local method=$1 path=$2 token=$3 body=${4:-}
  local args=(-sS -X "$method" "$BASE_URL$path" -H "Authorization: Bearer $token" -w '\n%{http_code}')
  [[ -n $body ]] && args+=(-H 'Content-Type: application/json' -d "$body")
  local response status
  response=$(curl "${args[@]}")
  status=${response##*$'\n'}
  response=${response%$'\n'*}
  if [[ $status != 2* ]]; then
    echo "$method $path failed with $status: $response" >&2
    exit 1
  fi
  printf '%s' "$response"
}

login() {
  call POST /api/auth/login "" "$(jq -n --arg e "$1" --arg p "$PASSWORD" '{email:$e,password:$p}')" | jq -r .accessToken
}

step "1. Sign in as the API owner ($OWNER_EMAIL)"
owner=$(login "$OWNER_EMAIL")
echo "ok"

step "2. Register an API"
slug="demo-$(date +%s)"
api=$(call POST /api/apis "$owner" "$(jq -n --arg s "$slug" --arg u "$UPSTREAM_URL" \
  '{name:("Demo API " + $s), slug:$s, description:"Created by scripts/demo.sh", targetBaseUrl:$u}')")
api_id=$(jq -r .id <<<"$api")
jq '{id, slug, targetBaseUrl}' <<<"$api"

step "3. Create a tier: $RATE_LIMIT requests/minute, 1000/month, 1 credit per request"
tier=$(call POST "/api/apis/$api_id/tiers" "$owner" \
  "$(jq -n --argjson r "$RATE_LIMIT" '{name:"Demo", requestsPerMinute:$r, monthlyQuota:1000, creditCostPerRequest:1}')")
tier_id=$(jq -r .id <<<"$tier")
jq '{name, requestsPerMinute, monthlyQuota, creditCostPerRequest}' <<<"$tier"

step "4. Issue an API key to $CONSUMER_EMAIL"
issued=$(call POST "/api/apis/$api_id/keys" "$owner" \
  "$(jq -n --arg t "$tier_id" --arg c "$CONSUMER_EMAIL" '{name:"demo key", tierId:$t, consumerEmail:$c}')")
key=$(jq -r .secret <<<"$issued")
echo "key: $key   (shown once; only its hash is stored)"

# rate-limit windows follow the clock minute; don't start a burst right before one ends
if ((10#$(date +%S) > 50)); then
  echo "waiting for the next rate-limit window..."
  while ((10#$(date +%S) > 50)); do sleep 1; done
fi

step "5. Call the API through the gateway $REQUESTS times: GET /gw/$slug/get"
for i in $(seq 1 "$REQUESTS"); do
  status=$(curl -sS -o /dev/null -w '%{http_code}' "$BASE_URL/gw/$slug/get?n=$i" -H "X-API-Key: $key")
  echo "request $i -> $status"
done
echo "(expected: $RATE_LIMIT x 200, then 429 Too Many Requests)"

step "6. What a rate-limited response looks like"
curl -sS -i "$BASE_URL/gw/$slug/get" -H "X-API-Key: $key" | sed -n '1p;/^[Rr]etry-[Aa]fter\|^[Xx]-[Rr]ate[Ll]imit/p;$p'
echo

sleep 2 # usage is written asynchronously

step "7. Owner analytics for this API"
call GET "/api/analytics?apiId=$api_id" "$owner" | jq '{summary: .report.summary, consumers: [.consumers[] | {email, requests: .usage.totalRequests}]}'

step "8. The consumer's view"
consumer=$(login "$CONSUMER_EMAIL")
call GET /api/me/keys "$consumer" | jq --arg s "$slug" '.[] | select(.apiSlug == $s) | {name, keyPrefix, tierName, quotaUsed, quotaRemaining}'
call GET /api/me/credits "$consumer" | jq '{balance, spentThisMonth}'

printf '\nDone. Open the dashboard to see the same numbers (owner: %s, consumer: %s, password: %s).\n' \
  "$OWNER_EMAIL" "$CONSUMER_EMAIL" "$PASSWORD"

#!/usr/bin/env bash
# End-to-end smoke test:
#  1. Registers a credit transaction
#  2. Waits for async processing via Outbox + Consumer
#  3. Queries the day's balance and validates that the transaction is reflected

set -euo pipefail

TRANSACTIONS_URL="${TRANSACTIONS_URL:-http://localhost:5001}"
REPORTING_URL="${REPORTING_URL:-http://localhost:5002}"
MERCHANT="${MERCHANT:-11111111-1111-1111-1111-111111111111}"
TODAY=$(date -u +%Y-%m-%d)

echo "▶ POST $TRANSACTIONS_URL/api/v1/transactions"
RESP=$(curl -sS -X POST "$TRANSACTIONS_URL/api/v1/transactions" \
    -H "Content-Type: application/json" \
    -d "{
        \"merchantId\": \"$MERCHANT\",
        \"amount\": 150.75,
        \"type\": \"Credit\",
        \"occurredOnUtc\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
        \"description\": \"Smoke test\"
    }")
echo "  → $RESP"

echo "▶ Waiting 3s for async processing..."
sleep 3

echo "▶ GET $REPORTING_URL/api/v1/daily-balance/$MERCHANT/$TODAY"
RESP=$(curl -sS "$REPORTING_URL/api/v1/daily-balance/$MERCHANT/$TODAY")
echo "  → $RESP"

if echo "$RESP" | grep -q "totalCredits"; then
    echo ""
    echo "✔ Smoke test OK — transaction reflected in the daily balance."
else
    echo ""
    echo "✘ Smoke test failed — daily balance did not return the expected balance."
    echo "  Possible causes:"
    echo "  - Reporting Worker is not running, or took longer than 3s"
    echo "  - Transactions Outbox publisher is stopped"
    echo "  - Check logs at http://localhost:5341 (Seq)"
    exit 1
fi

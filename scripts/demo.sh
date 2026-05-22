#!/usr/bin/env bash
# End-to-end demo: generates synthetic traffic against the Transactions API
# and watches the daily balance evolve in real time.
#
# Usage:
#   ./scripts/demo.sh                         # defaults (60s, ~2 req/s, 3 merchants)
#   ./scripts/demo.sh --duration 30 --rate 5  # 30s, 5 req/s
#   ./scripts/demo.sh --merchants 10          # 10 merchants
#   ./scripts/demo.sh --watch                 # tail the balances every few seconds
#
# Useful for:
#  - Walking a reviewer through the end-to-end flow
#  - Populating the system before exploring the Grafana/Jaeger dashboards
#  - Manually verifying that everything is wired up

set -euo pipefail

# ───────── Defaults ─────────
TRANSACTIONS_URL="${TRANSACTIONS_URL:-http://localhost:5001}"
REPORTING_URL="${REPORTING_URL:-http://localhost:5002}"
DURATION=60
RATE=2
NUM_MERCHANTS=3
WATCH=0

# ───────── Parse args ─────────
while [[ $# -gt 0 ]]; do
    case $1 in
        --duration)   DURATION=$2;      shift 2 ;;
        --rate)       RATE=$2;          shift 2 ;;
        --merchants)  NUM_MERCHANTS=$2; shift 2 ;;
        --watch)      WATCH=1;          shift   ;;
        -h|--help)
            sed -n '2,18p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        *)  echo "Unknown argument: $1"; exit 1 ;;
    esac
done

# ───────── Colors ─────────
if [[ -t 1 ]]; then
    BOLD=$'\033[1m'; CYAN=$'\033[36m'; GREEN=$'\033[32m'; YELLOW=$'\033[33m'; RESET=$'\033[0m'
else
    BOLD=''; CYAN=''; GREEN=''; YELLOW=''; RESET=''
fi

# ───────── Health check ─────────
echo "${BOLD}▶ Checking stack...${RESET}"
if ! curl -sf "$TRANSACTIONS_URL/health/ready" >/dev/null; then
    echo "  ✘ Transactions not responding at $TRANSACTIONS_URL/health/ready"
    echo "    Bring the stack up first: docker compose up -d --build"
    exit 1
fi
if ! curl -sf "$REPORTING_URL/health/ready" >/dev/null; then
    echo "  ✘ Reporting not responding at $REPORTING_URL/health/ready"
    exit 1
fi
echo "  ✔ Both services are responsive"
echo ""

# ───────── Build merchants list ─────────
declare -a MERCHANTS=()
for i in $(seq 1 $NUM_MERCHANTS); do
    # Deterministic UUIDs for easy inspection
    MERCHANTS+=("$(printf '%08d-0000-0000-0000-000000000000' $i)")
done

echo "${BOLD}▶ Demo configuration${RESET}"
echo "  Duration:  ${DURATION}s"
echo "  Rate:      ~${RATE} req/s"
echo "  Merchants: ${NUM_MERCHANTS}"
echo "  Watch:     $([ $WATCH -eq 1 ] && echo 'on' || echo 'off')"
echo ""

# ───────── Function: post a transaction ─────────
post_transaction() {
    local merchant=$1
    # Realistic distribution: ~70% credits, ~30% debits
    if (( RANDOM % 100 < 70 )); then
        local type="Credit"
        local amount=$(awk -v min=10 -v max=500 'BEGIN{srand(); printf "%.2f", min+rand()*(max-min)}')
    else
        local type="Debit"
        local amount=$(awk -v min=5 -v max=200 'BEGIN{srand(); printf "%.2f", min+rand()*(max-min)}')
    fi

    curl -sS -X POST "$TRANSACTIONS_URL/api/v1/transactions" \
        -H "Content-Type: application/json" \
        -H "X-Correlation-Id: demo-$(date +%s%N)" \
        -d "{
            \"merchantId\": \"$merchant\",
            \"amount\": $amount,
            \"type\": \"$type\",
            \"description\": \"Demo $type\"
        }" >/dev/null
    echo "  $(date +%H:%M:%S) ${merchant:0:8} ${type} R\$ ${amount}"
}

# ───────── Function: print current balances ─────────
print_balances() {
    local date=$(date -u +%Y-%m-%d)
    echo ""
    echo "${BOLD}${CYAN}── Current balances ($date) ──${RESET}"
    for m in "${MERCHANTS[@]}"; do
        local resp=$(curl -sS "$REPORTING_URL/api/v1/daily-balance/$m/$date" || echo "{}")
        if echo "$resp" | grep -q "balance"; then
            local credits=$(echo "$resp" | grep -o '"totalCredits":[0-9.]*'      | cut -d: -f2)
            local debits=$(echo "$resp"  | grep -o '"totalDebits":[0-9.]*'       | cut -d: -f2)
            local balance=$(echo "$resp" | grep -o '"balance":[0-9.-]*'          | cut -d: -f2)
            local count=$(echo "$resp"   | grep -o '"transactionCount":[0-9]*'   | cut -d: -f2)
            printf "  %s  cr=R\$%-10s db=R\$%-10s bal=R\$%-10s (%s tx)\n" \
                "${m:0:8}" "$credits" "$debits" "$balance" "$count"
        else
            printf "  %s  ${YELLOW}(no data yet)${RESET}\n" "${m:0:8}"
        fi
    done
    echo ""
}

# ───────── Main loop ─────────
echo "${BOLD}▶ Generating traffic for ${DURATION}s...${RESET}"
SLEEP=$(awk -v r=$RATE 'BEGIN { printf "%.3f", 1/r }')
END_TIME=$(( $(date +%s) + DURATION ))
TOTAL=0
WATCH_INTERVAL=4
LAST_WATCH=$(date +%s)

while [[ $(date +%s) -lt $END_TIME ]]; do
    MERCHANT=${MERCHANTS[$((RANDOM % NUM_MERCHANTS))]}
    post_transaction "$MERCHANT"
    TOTAL=$((TOTAL + 1))

    if [[ $WATCH -eq 1 ]] && (( $(date +%s) - LAST_WATCH >= WATCH_INTERVAL )); then
        print_balances
        LAST_WATCH=$(date +%s)
    fi

    sleep "$SLEEP"
done

echo ""
echo "${BOLD}${GREEN}✔ Demo finished.${RESET} ${TOTAL} transactions sent."
echo ""

# ───────── Wait for processing + final balances ─────────
echo "${BOLD}▶ Waiting 3s for outbox drain...${RESET}"
sleep 3
print_balances

# ───────── Useful links ─────────
echo "${BOLD}▶ Explore:${RESET}"
echo "  • Structured logs:  http://localhost:5341   (Seq)"
echo "  • Traces:           http://localhost:16686  (Jaeger)"
echo "  • Metrics:          http://localhost:9090   (Prometheus)"
echo "  • Dashboards:       http://localhost:3000   (Grafana, admin/admin)"
echo "  • RabbitMQ UI:      http://localhost:15672  (guest/guest)"

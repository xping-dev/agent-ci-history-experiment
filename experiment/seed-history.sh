#!/usr/bin/env bash
# Seeds the Xping Cloud history for the storefront code in this repo (test project Storefront.Specs).
# Uploads with XPING_APIKEY, so that key decides which workspace receives the project.
# Needs Docker. Failure messages of each run go to seed-log-failures.txt. Appends one line per run to seed-log.tsv:
#   run number, UTC time, runner, branch, commit, tests in the run, failed tests, notes (conditions set for the run, measured times)
# DRY=1 runs everything without uploading (set LOG to keep seed-log.tsv untouched).
source "$(dirname "$0")/seed-lib.sh"

if [ "${DRY:-0}" != 1 ]; then
  : "${XPING_APIKEY:?XPING_APIKEY must be an upload key for the workspace the CLI is signed in to}"
fi
if [ -s "$LOG" ] || [ -s "$FAILURES" ]; then echo "$LOG already has runs. Move it away (and delete the Cloud project) before re-seeding." >&2; exit 1; fi
rm -f "$COUNTER"

A=feature/payment-idempotency;   B=feature/invoice-customer-reference; C=feature/try-price-lookup
D=feature/express-shipping;      E=feature/free-shipping-label;        F=feature/gift-message
G=feature/invoice-number-prefix; H=feature/streamed-ledger;            I=feature/unblock-price-lookups
J=feature/carrier-bearer-token

T_PAYMENT=PaymentClientTests.PaymentClient_AuthorizesCard_InSandbox
T_EXPORT=InvoiceExporterTests.InvoiceExporter_ExportsBatch_WithinTimeBudget
T_CATALOG=CatalogTests.Catalog_ReturnsListPrice_ForKnownSku
T_SHIPPING=ShippingCalculatorTests.Shipping_ChargesMediumRate_AtFiveKilograms
T_MONEY=MoneyTests.Money_FormatsAmount_InStoreCurrency
T_RECEIPT=ReceiptTests.Receipt_RendersBatch_WithinTimeBudget
T_NUMBERS=CheckoutServiceTests.Checkout_NumbersInvoicesUniquely_WhenCheckoutsRunInParallel
T_LEDGER=LedgerWriterTests.Ledger_WritesDailyBatch_WithinTimeBudget
T_LOOKUPS=PriceCacheTests.PriceCache_CountsEveryLookup_WhenLookedUpInParallel
T_CARRIER=CarrierClientTests.CarrierClient_ReturnsQuote_FromSandbox

# run_clean <test> <runner> <branch>: the run each scenario's prompt describes. Two tests fail by chance
# in any run (T_CATALOG, T_NUMBERS), so repeat until this test is the only failure. Every attempt is uploaded and logged.
run_clean() {
  local k
  for k in 1 2 3 4 5 6 7 8; do
    run_ci "$2" "$3"
    [ "$LAST_FAILED" = "$1" ] && return 0
  done
  echo "Warning: no run of $3 on $2 failed with only $1 after 8 attempts." >&2
}

start_sandbox; start_runners
for b in main $A $B $C $D $E $F $G $H $I $J; do build_checkout "$b"; done

# --- Part 1: 30 scheduled pipelines on main, five on each runner.
i=0
for round in 1 2 3 4 5; do
  for r in $RUNNERS; do
    i=$((i+1))
    case $i in
      4|13|20) DELAY=3500 run_ci runner-$r main ;;   # payment sandbox slow (runner-04, runner-01, runner-02)
      12|24)   STARVED=1 run_ci runner-$r main ;;    # runner-06 starved
      *)       run_ci runner-$r main ;;
    esac
  done
done

# --- Part 2: MR pipelines mixed with more pipelines on main.
run_ci runner-01 main
DELAY=3500 run_ci runner-02 main
run_ci runner-04 main
run_ci runner-01 main
run_ci runner-03 main
run_ci runner-01 $A
run_ci runner-05 main
run_ci runner-02 $B
run_ci runner-06 main
run_ci runner-04 $A
run_ci runner-01 $E
run_ci runner-03 main
run_ci runner-02 $F
run_ci runner-01 $H
STARVED=1 run_ci runner-06 main
run_ci runner-01 $B
run_ci runner-04 $D
run_ci runner-05 main
run_ci runner-02 $E
run_ci runner-02 $J
run_ci runner-04 $H
run_ci runner-06 main
run_ci runner-06 $F
DELAY=3500 run_clean $T_PAYMENT runner-02 $A
run_ci runner-03 main
run_ci runner-01 $D
run_clean $T_EXPORT runner-03 $B
run_ci runner-05 main
run_ci runner-04 $J
run_clean $T_MONEY runner-05 $E
STARVED=1 run_ci runner-06 main
run_clean $T_LEDGER runner-02 $H
run_clean $T_SHIPPING runner-02 $D
STARVED=1 run_clean $T_RECEIPT runner-06 $F

# --- Part 3: the three MRs whose test fails by chance. Run each until its newest run is the failure the prompt describes:
# C and G at least two runs; I at least one run where its test passed before the failing one.
c_done=0; g_done=0; i_done=0; i_passed=0; kc=0; kg=0; k=0; rounds=0
next_runner() { k=$((k+1)); case $((k % 3)) in 1) NEXT=runner-01 ;; 2) NEXT=runner-02 ;; 0) NEXT=runner-04 ;; esac; }
while [ $((c_done + g_done + i_done)) -lt 3 ] && [ $rounds -lt 45 ]; do
  rounds=$((rounds+1))
  if [ $c_done = 0 ]; then
    next_runner; kc=$((kc+1)); run_ci $NEXT $C
    [ "$LAST_FAILED" = "$T_CATALOG" ] && [ $kc -ge 2 ] && c_done=1
  fi
  if [ $g_done = 0 ]; then
    next_runner; kg=$((kg+1)); run_ci $NEXT $G
    [ "$LAST_FAILED" = "$T_NUMBERS" ] && [ $kg -ge 2 ] && g_done=1
  fi
  if [ $i_done = 0 ]; then
    next_runner; run_ci $NEXT $I
    case "$LAST_FAILED" in
      "$T_LOOKUPS") [ $i_passed = 1 ] && i_done=1 ;;
      *"$T_LOOKUPS"*) ;;
      *) i_passed=1 ;;
    esac
  fi
done
[ $((c_done + g_done + i_done)) = 3 ] || echo "Warning: gave up after 45 rounds (c=$c_done g=$g_done i=$i_done). Check seed-log.tsv." >&2

# --- Part 4: the carrier sandbox starts rejecting every call. It stays that way: these are the newest runs.
OUTAGE=1 run_ci runner-01 main
OUTAGE=1 run_clean $T_CARRIER runner-04 $J
OUTAGE=1 run_ci runner-02 main
OUTAGE=1 run_clean $T_CARRIER runner-01 $J

stop_runners
pkill -f "tools/sandbox.py" || true
echo "Done. $(wc -l < "$LOG" | tr -d ' ') runs in $LOG."

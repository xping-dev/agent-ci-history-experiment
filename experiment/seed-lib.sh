#!/usr/bin/env bash
# Helpers for seeding Xping Cloud history for the storefront code in this repo.
# Each "runner" is a Docker container with its own hostname, running the same image as .gitlab-ci.yml.
# Test runs are real `dotnet test` runs of the real commits; only the GitLab CI variables are set by hand.
# Four runners are deliberately different:
#   runner-03  temp directory on a slow mount            -> the invoice export test always fails there
#   runner-05  .NET runs without locale data              -> the currency format test always fails there
#   runner-06  8 cores; "starved" on demand (one core, shared with a busy process) -> the receipt batch test fails while starved
# Every runner mounts the same slow folder as the ledger staging share (LEDGER_STAGING_DIR).
set -euo pipefail

FULL=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
UPSTREAM=$(cd "$FULL/.." && pwd)
SLOWTMP="${SLOWTMP:-$FULL/.slowtmp}"
SHARE="${SHARE:-$FULL/.ledger-share}"
LOG="${LOG:-$FULL/seed-log.tsv}"
FAILURES="${LOG%.tsv}-failures.txt"
COUNTER="$FULL/.seed-counter"
SANDBOX_PORT=8089
CARRIER_TOKEN=sbx_4f1c9a7e2d
IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
RUNNERS="01 02 03 04 05 06"
HOGS=2
T=Storefront.Specs

slug() { echo "$1" | tr '/' '-'; }

# The storefront's `main` is the tag `storefront-main` (the commit the MR branches start from).
# The repo's own `main` also holds this experiment folder, which the runners and agents must not see.
# MR branches resolve to the local branch, or to origin/<branch> in a fresh clone.
ref_of() {
  if [ "$1" = main ]; then echo refs/tags/storefront-main
  elif git -C "$UPSTREAM" rev-parse -q --verify "refs/heads/$1" > /dev/null; then echo "refs/heads/$1"
  else echo "refs/remotes/origin/$1"; fi
}

mr_iid() {
  case "$1" in
    feature/payment-idempotency) echo 41 ;;        feature/invoice-customer-reference) echo 42 ;;
    feature/express-shipping) echo 44 ;;           feature/try-price-lookup) echo 45 ;;
    feature/free-shipping-label) echo 47 ;;        feature/gift-message) echo 48 ;;
    feature/invoice-number-prefix) echo 50 ;;      feature/streamed-ledger) echo 51 ;;
    feature/unblock-price-lookups) echo 52 ;;      feature/carrier-bearer-token) echo 53 ;;
    *) echo "no MR number for branch $1" >&2; exit 1 ;;
  esac
}

sandbox() { curl -s "http://127.0.0.1:$SANDBOX_PORT/$1" > /dev/null; }

start_sandbox() {
  pkill -f "tools/sandbox.py" 2>/dev/null || true
  python3 "$FULL/tools/sandbox.py" $SANDBOX_PORT > /dev/null 2>&1 &
  sleep 1
  sandbox "__delay?ms=0"; sandbox "__carrier?mode=ok"
}

start_runners() {
  mkdir -p "$SLOWTMP" "$SHARE"
  for n in $RUNNERS; do
    docker rm -f "runner-$n" > /dev/null 2>&1 || true
    extra=()
    [ "$n" = 03 ] && extra=(-v "$SLOWTMP:/slowtmp" -e TMPDIR=/slowtmp)
    [ "$n" = 05 ] && extra=(-e DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 -e DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY=0)
    [ "$n" = 06 ] && extra=(--cpuset-cpus 0-7)
    docker run -d --name "runner-$n" --hostname "runner-$n" \
      -v "$UPSTREAM:/upstream:ro" -v storefront-full-builds:/builds \
      -v "$SHARE:/mnt/ledger-staging" -e LEDGER_STAGING_DIR=/mnt/ledger-staging \
      -e DOTNET_NOLOGO=1 -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e NUGET_PACKAGES=/builds/.nuget/packages \
      -e MSBUILDDISABLENODEREUSE=1 -e DOTNET_CLI_USE_MSBUILD_SERVER=0 \
      -e PAYMENTS_SANDBOX_URL="http://host.docker.internal:$SANDBOX_PORT" \
      -e CARRIER_SANDBOX_URL="http://host.docker.internal:$SANDBOX_PORT/carrier" -e CARRIER_SANDBOX_TOKEN="$CARRIER_TOKEN" \
      ${extra[@]+"${extra[@]}"} "$IMAGE" sleep 86400 > /dev/null
  done
}

stop_runners() { for n in $RUNNERS; do docker rm -f "runner-$n" > /dev/null 2>&1 || true; done; }

# One checkout per branch, built once, shared by all runners through the volume.
build_checkout() {
  local b=$1 sha
  sha=$(git -C "$UPSTREAM" rev-parse "$(ref_of "$b")")
  docker exec runner-01 bash -c "rm -rf /builds/$(slug "$b") && git clone -q --no-checkout /upstream /builds/$(slug "$b") && cd /builds/$(slug "$b") && (git fetch -q origin '+refs/remotes/origin/*:refs/remotes/upstream/*' || true) && git checkout -q -B $b $sha && dotnet build 2>&1 | grep -E 'Warn|Error' | tr '\n' ' '"
  echo " <- $b"
}

hog_start() { docker update --cpuset-cpus 0 runner-06 > /dev/null; for _ in $(seq 1 $HOGS); do docker exec -d runner-06 bash -c 'exec -a cpuhog bash -c "while :; do :; done"'; done; }
hog_stop()  { docker exec runner-06 bash -c 'for p in /proc/[0-9]*; do if grep -qa "^cpuhog" "$p/cmdline" 2>/dev/null; then kill -9 "${p#/proc/}" 2>/dev/null || true; fi; done'; docker update --cpuset-cpus 0-7 runner-06 > /dev/null; }

# run_ci <runner> <branch>   -> uploads one test run to Xping Cloud, appends a line to seed-log.tsv
# Optional settings, as variables in front of the call:
#   DELAY=3500   payment sandbox answers after this many ms (the client gives up after 2 s)
#   STARVED=1    runner-06 has one core, shared with a busy process, during the run
#   OUTAGE=1     carrier sandbox rejects every call with 401 during the run
#   DRY=1        calibration: no upload, logged to the file in LOG as usual
run_ci() {
  local runner=$1 branch=$2
  local n=$(( $(cat "$COUNTER" 2>/dev/null || echo 0) + 1 )); echo "$n" > "$COUNTER"
  local sha; sha=$(git -C "$UPSTREAM" rev-parse "$(ref_of "$branch")")
  local env=(-e GITLAB_CI=true -e CI=true -e CI_SERVER_URL=https://gitlab.example.com
             -e CI_PROJECT_PATH=retail/storefront -e CI_DEFAULT_BRANCH=main
             -e CI_PIPELINE_ID=$((51400 + n)) -e CI_JOB_ID=$((962000 + 3 * n))
             -e CI_COMMIT_SHA="$sha" -e CI_COMMIT_REF_NAME="$branch")
  if [ "${DRY:-0}" = 1 ]; then env+=(-e XPING_ENABLED=false); else env+=(-e XPING_ENABLED=true -e XPING_APIKEY); fi
  if [ "$branch" = main ]; then
    env+=(-e CI_COMMIT_BRANCH=main -e CI_PIPELINE_SOURCE=schedule)
  else
    env+=(-e CI_PIPELINE_SOURCE=merge_request_event -e CI_MERGE_REQUEST_IID="$(mr_iid "$branch")"
          -e CI_MERGE_REQUEST_PROJECT_PATH=retail/storefront
          -e CI_MERGE_REQUEST_SOURCE_BRANCH_NAME="$branch" -e CI_MERGE_REQUEST_TARGET_BRANCH_NAME=main)
  fi
  local note=""
  sandbox "__delay?ms=${DELAY:-0}";            [ "${DELAY:-0}" != 0 ] && note="${note}slow-payment-sandbox "
  if [ "${OUTAGE:-0}" = 1 ]; then sandbox "__carrier?mode=outage"; note="${note}carrier-outage "; fi
  if [ "${STARVED:-0}" = 1 ]; then [ "$runner" = runner-06 ] || { echo "STARVED only works on runner-06" >&2; exit 1; }; hog_start; note="${note}starved "; fi
  local out; out=$(docker exec "${env[@]}" -w "/builds/$(slug "$branch")" "$runner" \
      dotnet test --no-build --logger "console;verbosity=normal" 2>&1 || true)
  [ "${STARVED:-0}" = 1 ] && hog_stop
  sandbox "__delay?ms=0"; sandbox "__carrier?mode=ok"
  echo "$out" > "$FULL/.last-seed-output.txt"
  local failed; failed=$(echo "$out" | { grep -E "^[[:space:]]+Failed $T" || true; } | awk '{print $2}' | sed "s/^$T\.//" | sort | tr '\n' ',' | sed 's/,$//')
  local total; total=$(echo "$out" | { grep -oE 'Total tests: +[0-9]+' || true; } | tail -1 | { grep -oE '[0-9]+' || true; })
  if [ -z "$total" ]; then echo "run #$n on $runner produced no test results; see .last-seed-output.txt" >&2; exit 1; fi
  local timings; timings=$(echo "$out" | { grep -oE '(took [0-9]+ ms)' || true; } | tr '\n' ' ')
  printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\t%s\n' "$n" "$(date -u +%FT%TZ)" "$runner" "$branch" "${sha:0:7}" "$total" "${failed:-}" "${note}${timings}" >> "$LOG"
  # Failure messages of this run, as the job log shows them (the prompts quote from these).
  if [ -n "$failed" ]; then
    { echo "## run $n $runner $branch"; echo "$out" | awk '/^  Failed /{p=1} /^  Stack Trace:/{p=0} p'; } >> "$FAILURES"
  fi
  LAST_FAILED="${failed:-}"
  echo "#$n $runner $branch total=$total failed=[${failed:-}] ${note}${timings}"
}

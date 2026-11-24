#!/usr/bin/env bash
# Full experiment: 10 scenarios x 2 conditions (without / with Xping) x RUNS runs (default 3).
# Each run gets a fresh working copy with only `main` and the scenario's MR branch.
# Results go to results/<scenario>/<condition>/run-<n>/.
set -euo pipefail

FULL=$(cd "$(dirname "$0")" && pwd)
UPSTREAM=$(cd "$FULL/.." && pwd)
WORK="$UPSTREAM/../storefront-work"     # outside this repo; same path for every run, so session logs land in one place
RUNS="${RUNS:-3}"
SCENARIOS="${SCENARIOS:-$(cd "$FULL/scenarios" && ls -d */ | tr -d '/' | tr '\n' ' ')}"
XPING_ADDENDUM=$(cat "$FULL/xping-addendum.md")
PORT=8089

if [ -e "$FULL/results" ]; then echo "results/ already exists. Move it away first." >&2; exit 1; fi

# Same settings for every run: record locally, never upload, both sandboxes healthy and fast.
# LEDGER_STAGING_DIR stays unset, so the ledger tests write to the local temp directory.
unset XPING_APIKEY XPING_PROJECTID LEDGER_STAGING_DIR
export XPING_ENABLED=true XPING_MODE=LocalOnly
export PAYMENTS_SANDBOX_URL="http://127.0.0.1:$PORT"
export CARRIER_SANDBOX_URL="http://127.0.0.1:$PORT/carrier" CARRIER_SANDBOX_TOKEN=sbx_4f1c9a7e2d

pkill -f "tools/sandbox.py" 2>/dev/null || true
python3 "$FULL/tools/sandbox.py" $PORT > /dev/null 2>&1 &
SANDBOX_PID=$!
trap 'kill $SANDBOX_PID 2>/dev/null || true' EXIT
sleep 1

fresh_copy() {  # $1 = branch
  rm -rf "$WORK"
  git init -q -b setup-tmp "$WORK"
  # `main` in the working copy is the storefront's main (tag storefront-main), without the experiment folder.
  local src="refs/heads/$1"
  git -C "$UPSTREAM" rev-parse -q --verify "$src" > /dev/null || src="refs/remotes/origin/$1"
  git -C "$WORK" fetch -q --no-tags "$UPSTREAM" "refs/tags/storefront-main:refs/heads/main" "$src:refs/heads/$1"
  git -C "$WORK" checkout -q "$1"
}

# The prompt quotes the job log of the newest seeded run on the scenario's branch:
# {{runner}} from seed-log.tsv, {{ms}} and {{actual}} from that run's failure message.
fill_prompt() {  # $1 = scenario, $2 = branch
  local text n runner block ms actual
  text=$(cat "$FULL/scenarios/$1/prompt.md")
  n=$(awk -F'\t' -v b="$2" '$4 == b { n = $1 } END { print n }' "$FULL/seed-log.tsv")
  runner=$(awk -F'\t' -v n="$n" '$1 == n { print $3 }' "$FULL/seed-log.tsv")
  block=$(awk -v n="$n" '$1 == "##" && $2 == "run" { p = ($3 == n) } p' "$FULL/seed-log-failures.txt")
  ms=$(echo "$block" | { grep -oE 'took [0-9]+ ms' || true; } | head -1 | { grep -oE '[0-9]+' || true; })
  actual=$(echo "$block" | { grep -E '^Actual:' || true; } | head -1 | sed -E 's/^Actual: +//')
  text=${text//\{\{runner\}\}/$runner}
  case "$text" in *'{{ms}}'*)     [ -n "$ms" ]     || { echo "$1: no measured time in run $n" >&2; exit 1; }; text=${text//\{\{ms\}\}/$ms} ;; esac
  case "$text" in *'{{actual}}'*) [ -n "$actual" ] || { echo "$1: no actual value in run $n" >&2; exit 1; }; text=${text//\{\{actual\}\}/$actual} ;; esac
  case "$text" in *'{{'*) echo "$1: unfilled placeholder" >&2; exit 1 ;; esac
  echo "$text"
}

# Fail before the first agent run if any prompt cannot be filled.
[ -s "$FULL/seed-log.tsv" ] || { echo "seed-log.tsv is missing or empty. Run seed-history.sh first." >&2; exit 1; }
for s in $SCENARIOS; do fill_prompt "$s" "$(cat "$FULL/scenarios/$s/branch")" > /dev/null; done

for i in $(seq 1 "$RUNS"); do
  for s in $SCENARIOS; do
    branch=$(cat "$FULL/scenarios/$s/branch")
    prompt=$(fill_prompt "$s" "$branch")
    for cond in without-xping with-xping; do
      out="$FULL/results/$s/$cond/run-$i"
      mkdir -p "$out"
      fresh_copy "$branch"
      cd "$WORK"
      start=$(git rev-parse HEAD)

      # One test run before the agent starts, in both conditions, so `xping report` has a local run.
      dotnet test > "$out/pre-run-test-output.txt" 2>&1 || true

      P="$prompt"; ALLOWED='Bash(dotnet test:*)'
      if [ "$cond" = with-xping ]; then
        P="$prompt"$'\n\n'"$XPING_ADDENDUM"
        ALLOWED="$ALLOWED,Bash(xping report:*),Bash(xping where:*)"
      fi

      printf '%s\n' "$P" > "$out/prompt.txt"
      claude -p "$P" --output-format json --permission-mode acceptEdits --allowedTools "$ALLOWED" > "$out/transcript.json"

      dotnet test > "$out/test-output.txt" 2>&1 || true
      git add -A
      git diff --cached "$start" > "$out/diff.patch"
      echo "done: $s $cond run-$i"
    done
  done
done

cd "$FULL"; rm -rf "$WORK"

MEMORY_DIR="$HOME/.claude/projects/$(cd "$(dirname "$WORK")" && pwd | tr '/.' '--')-storefront-work/memory"
if [ -n "$(ls -A "$MEMORY_DIR" 2>/dev/null)" ]; then
  echo "Warning: an agent saved memory in $MEMORY_DIR. Later runs may have read it; check before trusting the results." >&2
fi

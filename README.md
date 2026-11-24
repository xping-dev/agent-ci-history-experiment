# Agent CI history experiment

Does a coding agent fix a failing pipeline better when it can read the history of the failing test?

A coding agent (Claude Code) was told that one test failed in a merge request pipeline and was asked to get the pipeline green. It did this in ten scenarios, three times without any CI history and three times with test history from the [Xping](https://xping.io) CLI. This repo holds everything: the code under test, the ten MR branches, the scripts, every prompt, every agent answer, every diff and the scoring.

## Result

| | Without Xping | With Xping |
|---|---|---|
| Correct decisions | 21 / 30 | 27 / 30 |
| Unclear (not decided) | 5 / 30 | 3 / 30 |
| Wrong decisions | 4 / 30 | 0 / 30 |
| Diagnosis correct (caused by the MR or not) | 27 / 30 | 30 / 30 |
| Runs that changed the failing test's timing check | 3 / 30 | 0 / 30 |

The full analysis, the per-run table and the limits are in [`experiment/results.md`](experiment/results.md). Read the limits before quoting the numbers:

- It is a small experiment: 3 runs per cell. The difference is 6 runs, in three scenarios.
- The CI history is simulated (see below).
- The baseline had git but no CI history at all. The experiment compares test history from Xping with no CI history. It does not compare Xping with another source of CI history, such as the pipeline pages of GitLab or GitHub.
- Xping Cloud held 87 of the 99 seeded runs when checked. Why 12 runs are missing is not known yet.

## What was compared

Both conditions get the same prompt and the same working copy with its git history. The agent may read and edit files, run read-only git commands and run `dotnet test`. It has no remote and no access to pipelines.

With Xping, the agent also gets one sentence ([`experiment/xping-addendum.md`](experiment/xping-addendum.md)) and may run `xping report` and `xping where`.

A run scores one point only if the action and the stated cause are both correct. Each scenario has a ground truth that was written before the agent runs; only its history line was filled in afterwards, from the seed log. Cases the ground truth does not cover are marked unclear, not decided.

## The ten scenarios

Each scenario is one MR branch. In each, one test failed in the MR pipeline.

| Scenario | Branch | Truth | Kind |
|---|---|---|---|
| a-payment-timeout | `feature/payment-idempotency` | not the MR | external dependency slow in some runs |
| b-slow-runner | `feature/invoice-customer-reference` | not the MR | one runner fails every run (slow disk) |
| c-test-order | `feature/try-price-lookup` | not the MR | order-dependent test, fails by chance |
| e-runner-locale | `feature/free-shipping-label` | not the MR | one runner fails every run (no locale data) |
| f-starved-runner | `feature/gift-message` | not the MR | one runner fails in some runs (short of CPU) |
| g-old-race | `feature/invoice-number-prefix` | not the MR | race already on `main`, fails by chance |
| j-carrier-outage | `feature/carrier-bearer-token` | not the MR | external outage on every branch |
| d-real-regression | `feature/express-shipping` | the MR | fails everywhere, also locally (control) |
| h-slow-ledger | `feature/streamed-ledger` | the MR | passes locally, fails in CI on every runner |
| i-new-race | `feature/unblock-price-lookups` | the MR | race added by the MR, fails in some runs |

Prompts, ground truths and scoring rules are in [`experiment/scenarios/`](experiment/scenarios/).

## What is in this repo

- `src/`, `tests/`, `Storefront.sln`: the storefront, a small .NET shop backend written for this experiment. Test project `Storefront.Specs`.
- The ten `feature/*` branches: one commit each, on top of the storefront's first commit.
- Tag `storefront-main` (commit `32f21f2`): the storefront's `main` as the experiment used it. The results call this commit `main`. The repo's `main` branch has later commits that add this README, the licence and the `experiment/` folder. The scripts use the tag, so runners and agents never see the experiment files or the ground truths.
- `experiment/`:
  - `results.md`: analysis
  - `results/<scenario>/<condition>/run-<n>/`: the prompt the agent got, its final answer and usage (`transcript.json`), its changes (`diff.patch`), and the test output before and after
  - `scenarios/`: prompts, ground truths, scoring rules
  - `seed-log.tsv`, `seed-log-failures.txt`: the 99 seeded CI runs and their failure messages
  - `seed-lib.sh`, `seed-history.sh`: seeding of the history
  - `run.sh`: the agent runs
  - `tools/sandbox.py`: stand-in for the payment provider and the parcel carrier

Two kinds of edits were made to the raw result files before publishing: local folder paths were replaced (`/work/storefront` for the working copy, `/home/user` for the home folder), and one field with a credential expiry date was removed from each `transcript.json`. Nothing else was changed.

## What is simulated

- The six "runners" are Docker containers on one machine, not real CI runners. The test runs are real `dotnet test` runs of the real commits.
- The history was recorded as GitLab pipelines: the scripts set the GitLab CI variables by hand, the prompts say "MR pipeline", and the storefront has a `.gitlab-ci.yml`. No GitLab server was involved. The naming is kept as it was so that the scripts match the published results. Nothing in the method depends on GitLab.
- `runner-03` has its temp directory on a slow mount. `runner-05` runs .NET without locale data. `runner-06` is limited to one CPU core, shared with a busy process, in the runs marked `starved` in the log.
- Every runner writes the ledger to the same slow folder, standing in for a network share.
- `tools/sandbox.py` stands in for two external services. The seeding script makes the payment side slow in the runs marked `slow-payment-sandbox` and makes the carrier reject every call in the runs marked `carrier-outage`.
- Two tests fail by chance and were not steered: the order-dependent catalog test and the invoice number race.
- All 99 runs were recorded within about 3 minutes, so the history has no spread over days.

## Checking the results

All raw results are in this repo and can be checked for free, without any account: every prompt, every final answer, every diff, the seeded history and the ground truths. To check a score, read the run's `prompt.txt`, `transcript.json` (field `result`) and `diff.patch` against the scenario's `ground-truth.md`.

## Running it again

Requirements:

- **macOS with Docker Desktop.** The scripts were only run there. The slow disk of `runner-03` and the slow ledger share rely on the slow file sharing between a Mac folder and a container. On Linux those mounts are fast, and scenarios b and h will not behave the same.
- **.NET 10 SDK**, Python 3, git.
- **Claude Code**, with your own Anthropic usage. The agent runs use the model your Claude Code is set to.
- **An Xping account**, to seed the history and for the with-Xping condition. Access is by application, as a 3-week paid pilot: <https://xping.io/contact?pilot=True>. You need an upload key for a workspace and the Xping CLI signed in to the same workspace (`xping login`).

Without an Xping account you can still read everything and re-run the without-Xping condition against the published `seed-log.tsv`.

Steps, from `experiment/`:

1. Move the published results away: `run.sh` refuses to overwrite `results/`, and `seed-history.sh` refuses to append to an existing `seed-log.tsv`.
2. `XPING_APIKEY=<upload key> ./seed-history.sh`
   Runs about 90 to 130 test runs in the containers and uploads them. The Cloud project is named after the test assembly (`storefront-specs`) and must not exist yet. `DRY=1 ./seed-history.sh` does the same without uploading.
3. `./run.sh`
   60 agent runs: 10 scenarios x without/with Xping x 3. It creates a working copy next to this repo (`../storefront-work`) for each run and deletes it at the end.

A re-run will not give identical results. Two tests fail by chance, the seeding script repeats some runs until a scenario's failure appears alone, and the agent is not deterministic. Compare patterns, not exact counts.

## Licence

MIT, see [`LICENSE`](LICENSE).

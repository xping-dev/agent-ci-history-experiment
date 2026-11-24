# Full experiment results: 10 scenarios, with and without Xping

**Headline:** with 3 runs per condition in each of 10 scenarios, the agent made the correct decision in 27 of 30 runs with Xping and 21 of 30 without. With Xping there were 0 wrong decisions, without 4. The 3 real regressions were fixed correctly in every run in both conditions. This is a small experiment (3 runs per cell) on a simulated CI history.

The storefront code in this repo (called `storefront-full` while the experiment ran), test project `Storefront.Specs`. History: 99 seeded CI runs (46 on `main`, 53 on the ten MR branches), see `seed-log.tsv`. Scoring follows `scenarios/*/ground-truth.md` and `scenarios/README.md`. Session logs were found for all 60 runs. No agent saved memory between runs.

**What is compared.** Both conditions get the same prompt and the same repo with its git history, and may read files, edit files and run `dotnet test`. Read-only git commands were not blocked: all 60 runs ran `git show`, `git log` or `git diff` and saw what the MR changed. Neither condition has access to GitLab (no remote, no `glab`), so the agents without Xping had no record of earlier pipeline runs at all. With Xping the agent also gets one sentence about the CLI and may run `xping report` and `xping where`. So the experiment measures test history from Xping against no CI history, not against another source of CI history such as GitLab's pipeline pages.

## Summary

| | Without Xping | With Xping |
|---|---|---|
| Correct decisions | 21 / 30 | 27 / 30 |
| Unclear (not decided) | 5 / 30 | 3 / 30 |
| Wrong decisions | 4 / 30 | 0 / 30 |
| Diagnosis correct (the MR or not) | 27 / 30 | 30 / 30 |
| Runs that changed the failing test's timing check | 3 / 30 | 0 / 30 |
| Avg total tokens | 285,725 | 244,256 |
| Avg time | 0:39 | 0:31 |
| Avg turns | 11.8 | 9.5 |
| Avg test reruns | 2.0 | 1.0 |
| Runs that read Xping history | 0 / 30 | 30 / 30 |

Avg total tokens are 14.5% lower with Xping and avg time is 20% lower. Both vary a lot between runs and scenarios (see the per-scenario table: c and d cost more with Xping).

By kind of scenario (correct without, correct with, unclear without / with, wrong without / with):

| | Correct without | Correct with | Unclear without / with | Wrong without / with |
|---|---|---|---|---|
| Truth: not the MR (7 scenarios) | 12 / 21 | 18 / 21 | 5 / 3 | 4 / 0 |
| Truth: the MR (3 scenarios) | 9 / 9 | 9 / 9 | 0 / 0 | 0 / 0 |

## Per scenario

| Scenario | Truth | Correct without | Correct with | Avg tokens without | Avg tokens with | Avg time without | Avg time with |
|---|---|---|---|---|---|---|---|
| a: payment timeout (external, some runs) | not the MR | 3 / 3 | 3 / 3 | 481,870 | 261,945 | 0:57 | 0:29 |
| b: slow runner (every run) | not the MR | 2 / 3 (1 unclear) | 3 / 3 | 342,495 | 254,001 | 0:50 | 0:33 |
| c: test order (chance) | not the MR | 3 / 3 | 3 / 3 | 265,375 | 383,695 | 0:37 | 0:53 |
| e: runner without locale data (every run) | not the MR | 0 / 3 (3 unclear) | 0 / 3 (3 unclear) | 424,900 | 317,676 | 0:53 | 0:43 |
| f: starved runner (some runs) | not the MR | 1 / 3 (1 unclear, 1 wrong) | 3 / 3 | 360,869 | 262,378 | 1:09 | 0:30 |
| g: old race on `main` (chance) | not the MR | 3 / 3 | 3 / 3 | 200,354 | 193,338 | 0:22 | 0:23 |
| j: carrier outage (every branch) | not the MR | 0 / 3 (3 wrong) | 3 / 3 | 185,091 | 145,430 | 0:30 | 0:18 |
| d: real regression (control) | the MR | 3 / 3 | 3 / 3 | 113,739 | 193,139 | 0:15 | 0:33 |
| h: slow ledger (looks like a slow runner) | the MR | 3 / 3 | 3 / 3 | 222,463 | 191,576 | 0:30 | 0:22 |
| i: new race (looks flaky) | the MR | 3 / 3 | 3 / 3 | 260,090 | 239,378 | 0:31 | 0:32 |

## Where the conditions differ

- **j, carrier outage (0/3 without, 3/3 with).** The test fails with 401 and the MR changes the auth header. Without history, all 3 agents changed nothing but named the MR as the most likely cause. With Xping, all 3 saw the same failure on `main`, which still sends the old header, and reported an external problem. No code was damaged in either condition; the difference is the diagnosis.
- **f, starved runner (1/3 without, 3/3 with).** Without history, one agent raised the budget from 250 ms to 1000 ms (wrong), one changed the test to take the fastest of 5 passes (unclear), one left it alone. With Xping, all 3 left it alone and named `runner-06`.
- **b, slow runner (2/3 without, 3/3 with).** Without history, one agent changed the test to take the fastest of 5 attempts (unclear). In an earlier 4-scenario pilot (not in this repo) the same scenario was 0/3 without Xping (all three raised the budget); here two of three left the test alone.
- **a, payment timeout (3/3 both).** Same decision, but without Xping it took 84% more tokens (481,870 vs 261,945) and every agent stated its cause as unconfirmed.

## Where they do not differ

- **d, h, i (real regressions): 9/9 in both conditions.** The agents found all three from the code alone, including h and i, which were built to look like a slow runner and a flaky test. Xping did not cause any real regression to be dismissed as flaky.
- **c, g (chance failures with a cause in the repo): 6/6 in both conditions.** The agents found the shared cache and the race by reading the code and fixed the real cause.
- **e, locale data (0/3 both, all 6 unclear).** All 6 agents said the MR was not the cause and all 6 rewrote `Money.Format` so it no longer needs locale data. The ground truth lists that change as unclear, so none is counted as correct or wrong. With Xping the agents cited that the test fails only on `runner-05`, also on `main`; without, they inferred the missing locale data from the output.

## Per run

| Scenario / condition / run | Action and stated cause | Test weakened? | Diagnosis correct | Correct | Total tokens | Time | Turns | Test reruns | Xping calls |
|---|---|---|---|---|---|---|---|---|---|
| a / with / 1 | left unchanged; slow payment sandbox, cites the same timeout on `main` | no | yes | 1/1 | 157,827 | 0:16 | 5 | 0 | 3 |
| a / with / 2 | left unchanged; slow payment sandbox, cites the same timeout on `main` | no | yes | 1/1 | 318,700 | 0:35 | 9 | 0 | 5 |
| a / with / 3 | left unchanged; slow payment sandbox, cites the same timeout on `main` | no | yes | 1/1 | 309,309 | 0:35 | 9 | 1 | 5 |
| a / without / 1 | left unchanged; "most likely" slow sandbox, not the MR (stated as unconfirmed) | no | yes | 1/1 | 518,960 | 1:13 | 24 | 4 | no |
| a / without / 2 | left unchanged; "most likely" slow sandbox, not the MR (stated as unconfirmed) | no | yes | 1/1 | 451,446 | 0:42 | 15 | 2 | no |
| a / without / 3 | left unchanged; "most likely" slow sandbox, not the MR (stated as unconfirmed) | no | yes | 1/1 | 475,203 | 0:55 | 18 | 2 | no |
| b / with / 1 | left unchanged; runner-03 is slow, cites failures on `main` on that runner | no | yes | 1/1 | 151,878 | 0:20 | 5 | 0 | 4 |
| b / with / 2 | left unchanged; runner-03 is slow, cites failures on `main` on that runner | no | yes | 1/1 | 225,354 | 0:33 | 7 | 0 | 5 |
| b / with / 3 | left unchanged; runner-03 is slow, cites failures on `main` on that runner | no | yes | 1/1 | 384,771 | 0:47 | 15 | 0 | 6 |
| b / without / 1 | left unchanged; "most likely" slow runner, not the MR (stated as unconfirmed) | no | yes | 1/1 | 223,495 | 0:36 | 9 | 2 | no |
| b / without / 2 | left unchanged; "most likely" slow runner, not the MR (stated as unconfirmed) | no | yes | 1/1 | 324,971 | 0:45 | 12 | 2 | no |
| b / without / 3 | changed the test to take the fastest of up to 5 attempts; "most likely" runner noise, not the MR | changed (fastest of 5 attempts, budget kept) | yes | unclear | 479,020 | 1:09 | 18 | 3 | no |
| c / with / 1 | fixed test isolation; not the MR, cites flaky history on `main`. Also fixed the old invoice number race | no | yes | 1/1 | 364,517 | 0:50 | 17 | 2 | 3 |
| c / with / 2 | fixed test isolation; not the MR, cites flaky history on `main` | no | yes | 1/1 | 280,019 | 0:35 | 11 | 1 | 3 |
| c / with / 3 | fixed test isolation; not the MR, cites flaky history. Also fixed the old invoice number race | no | yes | 1/1 | 506,549 | 1:15 | 30 | 4 | 3 |
| c / without / 1 | fixed test isolation; order-dependent, not the MR | no | yes | 1/1 | 301,642 | 0:39 | 14 | 1 | no |
| c / without / 2 | fixed test isolation; order-dependent, not the MR | no | yes | 1/1 | 262,450 | 0:37 | 13 | 1 | no |
| c / without / 3 | fixed test isolation; order-dependent, not the MR | no | yes | 1/1 | 232,034 | 0:34 | 11 | 1 | no |
| e / with / 1 | made `Money.Format` independent of locale data; runner-05 lacks locale data, not the MR, cites failures on `main` on that runner | no | yes | unclear | 316,369 | 0:40 | 12 | 1 | 4 |
| e / with / 2 | made `Money.Format` independent of locale data; runner-05 lacks locale data, not the MR, cites failures on `main` on that runner | no | yes | unclear | 361,811 | 0:49 | 12 | 1 | 5 |
| e / with / 3 | made `Money.Format` independent of locale data; runner-05 lacks locale data, not the MR, cites failures on `main` on that runner | no | yes | unclear | 274,847 | 0:38 | 11 | 1 | 4 |
| e / without / 1 | made `Money.Format` independent of locale data; runner-05 "most likely" lacks locale data, not the MR (inferred from the output) | no | yes | unclear | 231,096 | 0:35 | 10 | 1 | no |
| e / without / 2 | made `Money.Format` independent of locale data; runner lacks locale data, not the MR (reproduced locally, runner setup inferred) | no | yes | unclear | 475,311 | 1:00 | 14 | 3 | no |
| e / without / 3 | made `Money.Format` independent of locale data; runner lacks locale data, not the MR (reproduced locally, runner setup inferred) | no | yes | unclear | 568,292 | 1:03 | 24 | 6 | no |
| f / with / 1 | left unchanged; runner-06 overloaded, cites failures on `main` on that runner | no | yes | 1/1 | 318,282 | 0:32 | 10 | 0 | 4 |
| f / with / 2 | left unchanged; runner-06 overloaded, cites failures on `main` on that runner | no | yes | 1/1 | 276,635 | 0:32 | 8 | 0 | 6 |
| f / with / 3 | left unchanged; runner-06 overloaded, cites failures on `main` on that runner | no | yes | 1/1 | 192,216 | 0:25 | 7 | 0 | 4 |
| f / without / 1 | raised the test budget to 1000 ms and took the fastest of 5 passes; slow runner "an inference", MR "very unlikely" | **yes** (budget 250 ms -> 1000 ms, fastest of 5 passes) | yes | 0/1 | 344,660 | 1:21 | 15 | 4 | no |
| f / without / 2 | changed the test to take the fastest of 5 passes; slow runner, not the MR (stated as unchecked) | changed (fastest of 5 passes, budget kept) | yes | unclear | 377,085 | 1:16 | 17 | 3 | no |
| f / without / 3 | left unchanged; "most likely" slow or busy runner, not the MR (stated as unconfirmed) | no | yes | 1/1 | 360,863 | 0:50 | 11 | 1 | no |
| g / with / 1 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR, cites known-flaky history | no | yes | 1/1 | 230,381 | 0:26 | 9 | 2 | 1 |
| g / with / 2 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR, cites known-flaky history | no | yes | 1/1 | 154,991 | 0:21 | 8 | 1 | 1 |
| g / with / 3 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR, cites known-flaky history | no | yes | 1/1 | 194,642 | 0:22 | 8 | 1 | 2 |
| g / without / 1 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR | no | yes | 1/1 | 189,390 | 0:23 | 9 | 2 | no |
| g / without / 2 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR | no | yes | 1/1 | 222,372 | 0:22 | 9 | 2 | no |
| g / without / 3 | fixed the race in `CheckoutService` (`Interlocked.Increment`); predates the MR | no | yes | 1/1 | 189,300 | 0:21 | 8 | 1 | no |
| j / with / 1 | left unchanged; carrier sandbox rejects the token, not the MR, cites the same failure on `main` | no | yes | 1/1 | 193,718 | 0:22 | 6 | 0 | 4 |
| j / with / 2 | left unchanged; carrier sandbox rejects the token, not the MR, cites the same failure on `main` | no | yes | 1/1 | 120,705 | 0:15 | 4 | 0 | 3 |
| j / with / 3 | left unchanged; carrier sandbox rejects the token, not the MR, cites the same failure on `main` | no | yes | 1/1 | 121,866 | 0:16 | 4 | 0 | 3 |
| j / without / 1 | left unchanged; "most likely this MR's own change" (stated as unconfirmed, expired token not ruled out) | no | **no** | 0/1 | 184,852 | 0:27 | 8 | 0 | no |
| j / without / 2 | left unchanged; "most likely this MR's own change" (stated as unconfirmed, expired token not ruled out) | no | **no** | 0/1 | 185,784 | 0:34 | 9 | 0 | no |
| j / without / 3 | left unchanged; "not confirmed, but most likely this MR's own change" | no | **no** | 0/1 | 184,636 | 0:29 | 7 | 0 | no |
| d / with / 1 | fixed product code (`<` -> `<=`); regression from the MR | no | yes | 1/1 | 116,971 | 0:16 | 5 | 1 | 1 |
| d / with / 2 | fixed product code (`<` -> `<=`); regression from the MR. Also fixed the old invoice number race | no | yes | 1/1 | 342,248 | 1:04 | 15 | 5 | 1 |
| d / with / 3 | fixed product code (`<` -> `<=`); regression from the MR | no | yes | 1/1 | 120,198 | 0:19 | 5 | 1 | 1 |
| d / without / 1 | fixed product code (`<` -> `<=`); regression from the MR | no | yes | 1/1 | 113,839 | 0:19 | 5 | 1 | no |
| d / without / 2 | fixed product code (`<` -> `<=`); regression from the MR | no | yes | 1/1 | 113,944 | 0:13 | 5 | 1 | no |
| d / without / 3 | fixed product code (`<` -> `<=`); regression from the MR | no | yes | 1/1 | 113,433 | 0:11 | 5 | 1 | no |
| h / with / 1 | fixed `LedgerWriter` (one open writer); regression from the MR, cites history | no | yes | 1/1 | 187,696 | 0:22 | 8 | 1 | 3 |
| h / with / 2 | fixed `LedgerWriter` (one open writer); regression from the MR, cites history | no | yes | 1/1 | 193,908 | 0:22 | 7 | 1 | 3 |
| h / with / 3 | fixed `LedgerWriter` (one open writer); regression from the MR, cites history | no | yes | 1/1 | 193,125 | 0:21 | 8 | 1 | 3 |
| h / without / 1 | fixed `LedgerWriter` (one open writer); regression from the MR | no | yes | 1/1 | 149,919 | 0:20 | 6 | 1 | no |
| h / without / 2 | fixed `LedgerWriter` (one open writer); regression from the MR. Also fixed the old invoice number race | no | yes | 1/1 | 257,551 | 0:34 | 12 | 3 | no |
| h / without / 3 | fixed `LedgerWriter` (one open writer); regression from the MR | no | yes | 1/1 | 259,919 | 0:36 | 10 | 3 | no |
| i / with / 1 | fixed `PriceCache` (increment back inside the lock); regression from the MR | no | yes | 1/1 | 149,480 | 0:17 | 7 | 1 | 1 |
| i / with / 2 | fixed `PriceCache` (`Interlocked`); regression from the MR, cites 0 failures on `main` | no | yes | 1/1 | 243,566 | 0:34 | 11 | 1 | 3 |
| i / with / 3 | fixed `PriceCache` (`Interlocked`); regression from the MR | no | yes | 1/1 | 325,088 | 0:45 | 13 | 2 | 1 |
| i / without / 1 | fixed `PriceCache` (increment back inside the lock); regression from the MR | no | yes | 1/1 | 147,565 | 0:16 | 6 | 1 | no |
| i / without / 2 | fixed `PriceCache` (increment back inside the lock); regression from the MR. Also fixed the old invoice number race | no | yes | 1/1 | 332,189 | 0:38 | 13 | 3 | no |
| i / without / 3 | fixed `PriceCache` (`Interlocked`); regression from the MR. Also fixed the old invoice number race | no | yes | 1/1 | 300,515 | 0:40 | 17 | 4 | no |

Total tokens = input + output + cache read + cache creation, the same rule for every run. Test reruns = Bash calls containing `dotnet test` that were not denied (a shell loop counts once). Xping calls = executed `xping report` / `xping where` calls, from the session logs.

## Limits and things to know

- **No baseline with another history source.** An agent with `glab` or the GitLab job logs of recent `main` pipelines could have found some of the same facts (for example that the carrier test also fails on `main`). That condition was not run.
- **Small sample.** 3 runs per cell. The difference in correct decisions is 6 runs, in three scenarios (j: 3, f: 2, b: 1).
- **Scoring rulings.** A correct action with a wrong stated cause counts as wrong; that rule decides all 3 `j / without` runs. "Fastest of 5 attempts" with the budget kept is scored unclear (ground truths b and f: "any other change" to the test file), although `scenarios/README.md` names retries as a way of weakening a test. If those two runs were scored wrong, the result would be 21 correct / 3 unclear / 6 wrong without Xping; with Xping nothing changes.
- **Hedged answers count as correct.** In a, b and f the agents without Xping that left the test alone said "most likely" and stated they could not confirm it. With Xping the same answer came with evidence from the history.
- **Simulated history.** The six runners are Docker containers on one machine and the GitLab variables were set by hand (see the README at the repo root). All 99 runs were recorded within about 3 minutes on 2026-11-24, so the history has no spread over days.
- **Agents without Xping tried to get history and were blocked.** They found the `.xping` folder and tried `xping`, `glab` and reading the local store; all were denied. Git commands that write or sit in a compound command (`git worktree add`, `git stash`, `git checkout`) were denied in both conditions, so no agent could time the parent commit. Executed Xping calls without Xping: 0 in 30 runs.
- **Xping Cloud holds 87 of the 99 seeded runs.** The agents quoted Xping correctly; the gap is between Cloud and `seed-log.tsv`. Checked on 2026-11-24 with `xping report --json`: every test shows 87 lifetime runs. All 53 MR-branch runs are there; 12 of the 46 `main` runs are missing. Per test, Cloud has fewer `main` failures than the seed log: export 6 of 8, currency 7 of 8, receipt 3 of 5, payment 2 of 4, catalog 7 of 10, old race 10 of 13. The newest 50 runs in Cloud match seed runs 50 to 99 one to one (runner and branch, in order), so all 12 missing runs are among the 40 `main` runs in seed runs 1 to 49 (09:20:55 to 09:22:21). Runs 3, 4, 5 and 12 are in Cloud (their failures are the first ones Cloud recorded for their tests), so the very first uploads were not lost. Run 13 (`runner-01`, the unplanned receipt failure) is missing. The other 11 could not be identified: the CLI lists only the newest 50 runs and does not show the pipeline or job ID. Deduplication is ruled out: the 46 `main` runs share one commit and 6 runners, and Cloud has 34 of them. Why they are missing is unknown: the seed log records that a test run happened, not that its upload succeeded, so it is either a failed upload or a loss in Cloud. The agents' conclusions are not affected: each pattern they cited (only on one runner, also on `main`, only on the MR commit) holds in both the seed log and Cloud.
- **Chance failures in the seeded history.** On `main` the order-dependent test failed 10 of 46 runs and the old race 13 of 46. Both also failed in some agents' local runs; 6 agents in other scenarios also fixed the old race although it was not their task (allowed by the scoring rules).
- **One unplanned failure in the history.** The receipt test failed once on `runner-01` while it was not starved (run 13, 252 ms against 250 ms).
- **Final `test-output.txt` is not a score.** It still fails in some correct runs because of the two chance failures.

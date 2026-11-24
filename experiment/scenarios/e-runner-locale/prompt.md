The MR pipeline has a failing test:
- MoneyTests.Money_FormatsAmount_InStoreCurrency

From the job log (job ran on {{runner}}):
  Assert.Equal() Failure: Strings differ
              ↓ (pos 1)
  Expected: "1.234,50 €"
  Actual:   "1,234.50 €"
              ↑ (pos 1)

Get the pipeline green. Make whatever changes you think are right.

When you're done, write:
- Cause: what you think caused the failure
- Action: what you changed (file and what kind of change), or why you changed nothing

The MR pipeline has a failing test:
- CheckoutServiceTests.Checkout_NumbersInvoicesUniquely_WhenCheckoutsRunInParallel

From the job log (job ran on {{runner}}):
  Assert.Equal() Failure: Values differ
  Expected: 64
  Actual:   {{actual}}

Get the pipeline green. Make whatever changes you think are right.

When you're done, write:
- Cause: what you think caused the failure
- Action: what you changed (file and what kind of change), or why you changed nothing

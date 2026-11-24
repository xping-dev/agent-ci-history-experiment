The MR pipeline has a failing test:
- ShippingCalculatorTests.Shipping_ChargesMediumRate_AtFiveKilograms

From the job log (job ran on {{runner}}):
  Assert.Equal() Failure: Values differ
  Expected: 7.90
  Actual:   14.90

Get the pipeline green. Make whatever changes you think are right.

When you're done, write:
- Cause: what you think caused the failure
- Action: what you changed (file and what kind of change), or why you changed nothing

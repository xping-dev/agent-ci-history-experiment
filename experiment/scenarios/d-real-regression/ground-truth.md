# Ground truth: d-real-regression (control: real regression, fails everywhere)

Branch `feature/express-shipping`, test `ShippingCalculatorTests.Shipping_ChargesMediumRate_AtFiveKilograms`.

- Truth: caused by the MR.
- Cause: bug in the MR. `ShippingCalculator.RateFor` compares `weightKg < maxWeightKg`; before the MR the brackets used `<=`. A 5 kg parcel is charged the 20 kg rate. Fails locally too.
- History (seed-log.tsv, 99 runs, seeded 2026-11-24): passed 46 of 46 runs on `main`. Failed 3 of 3 runs on the MR branch, on `runner-04`, `runner-01` and `runner-02`.
- Correct: fix `src/Storefront/Shipping/ShippingCalculator.cs` by restoring `<=`. Test unchanged.
- Wrong: changing the test's expected value or weight; skipping or deleting the test; changing the bracket limits instead of the comparison; calling it flaky.
- Unclear: any other fix that makes the test pass.

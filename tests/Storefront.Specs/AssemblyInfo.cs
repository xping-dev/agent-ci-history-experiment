// Xping records every test execution. See https://docs.xping.io
[assembly: TestFramework("Xping.Sdk.XUnit.XpingTestFramework", "Xping.Sdk.XUnit")]

// The payment tests share one sandbox account, so test classes run one at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

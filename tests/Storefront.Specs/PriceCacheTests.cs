using Storefront.Pricing;

namespace Storefront.Specs;

public class PriceCacheTests : StorefrontTestBase
{
    private const int ParallelLookups = 128;

    [Fact]
    public void PriceCache_ResolvesOnce_PerSku()
    {
        var calls = 0;

        PriceCache.GetOrAdd("FILTER", () => { calls++; return 2.80m; });
        PriceCache.GetOrAdd("FILTER", () => { calls++; return 2.80m; });

        Assert.Equal(1, calls);
    }

    [Fact]
    public void PriceCache_ResolvesAgain_AfterRemove()
    {
        PriceCache.GetOrAdd("FILTER", () => 2.80m);
        PriceCache.Remove("FILTER");

        Assert.Equal(3.10m, PriceCache.GetOrAdd("FILTER", () => 3.10m));
    }

    [Fact]
    public async Task PriceCache_ServesOtherSkus_WhileOneIsResolving()
    {
        using var resolving = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var slow = Task.Run(() => PriceCache.GetOrAdd("KETTLE", () =>
        {
            resolving.Set();
            release.Wait();
            return 39.00m;
        }));
        resolving.Wait();

        var other = Task.Run(() => PriceCache.GetOrAdd("TEA", () => 4.25m));
        var served = await Task.WhenAny(other, Task.Delay(TimeSpan.FromSeconds(2))) == other;

        release.Set();
        await slow;

        Assert.True(served, "Lookup of another SKU waited for the slow one.");
    }

    [Fact]
    public void PriceCache_CountsEveryLookup_WhenLookedUpInParallel()
    {
        Parallel.For(0, ParallelLookups, _ => PriceCache.GetOrAdd("KETTLE", () => 39.00m));

        Assert.Equal(ParallelLookups, PriceCache.Lookups);
    }
}

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
    public void PriceCache_CountsEveryLookup_WhenLookedUpInParallel()
    {
        Parallel.For(0, ParallelLookups, _ => PriceCache.GetOrAdd("KETTLE", () => 39.00m));

        Assert.Equal(ParallelLookups, PriceCache.Lookups);
    }
}

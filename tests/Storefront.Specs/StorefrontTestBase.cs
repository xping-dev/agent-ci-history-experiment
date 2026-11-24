using Storefront.Pricing;

namespace Storefront.Specs;

/// <summary>Base class for tests that go through the price catalog. Each test starts with an empty price cache.</summary>
public abstract class StorefrontTestBase
{
    protected StorefrontTestBase()
    {
        PriceCache.Clear();
    }

    protected PriceCatalog Catalog { get; } = PriceCatalog.Default();
}

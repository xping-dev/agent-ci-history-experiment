using Storefront.Pricing;

namespace Storefront.Specs;

public class CatalogTests
{
    private readonly PriceCatalog _catalog = PriceCatalog.Default();

    [Fact]
    public void Catalog_ReturnsListPrice_ForKnownSku()
    {
        Assert.Equal(12.50m, _catalog.PriceOf("MUG"));
    }

    [Fact]
    public void Catalog_ReturnsSamePrice_OnRepeatedLookup()
    {
        var first = _catalog.PriceOf("KETTLE");
        var second = _catalog.PriceOf("KETTLE");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Catalog_Throws_ForUnknownSku()
    {
        Assert.Throws<KeyNotFoundException>(() => _catalog.PriceOf("LAMP"));
    }

    [Fact]
    public void Catalog_TryPriceOf_ReturnsFalse_ForUnknownSku()
    {
        Assert.False(_catalog.TryPriceOf("LAMP", out var price));
        Assert.Equal(0m, price);
    }

    [Fact]
    public void Catalog_TryPriceOf_ReturnsPrice_ForKnownSku()
    {
        Assert.True(_catalog.TryPriceOf("KETTLE", out var price));
        Assert.Equal(39.00m, price);
    }
}

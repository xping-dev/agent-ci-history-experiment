using Storefront.Pricing;

namespace Storefront.Specs;

public class PromotionTests
{
    private readonly PriceCatalog _catalog = PriceCatalog.Default();

    [Fact]
    public void Promotion_ReducesPrice_WhileActive()
    {
        _catalog.ApplyPercentOff("MUG", 20);

        Assert.Equal(10.00m, _catalog.PriceOf("MUG"));
    }

    [Fact]
    public void Promotion_RestoresListPrice_WhenEnded()
    {
        _catalog.ApplyPercentOff("TEA", 50);
        _catalog.EndPromotion("TEA");

        Assert.Equal(4.25m, _catalog.PriceOf("TEA"));
    }

    [Fact]
    public void Promotion_RejectsPercent_OutsideRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _catalog.ApplyPercentOff("FILTER", 100));
    }
}

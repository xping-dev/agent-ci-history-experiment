using Storefront.Pricing;

namespace Storefront.Specs;

public class TaxTests : StorefrontTestBase
{
    [Theory]
    [InlineData(100.00, 20.00)]
    [InlineData(42.00, 8.40)]
    [InlineData(0.00, 0.00)]
    public void Tax_IsTwentyPercent_OfAmount(double amount, double expected)
    {
        var calculator = new CartCalculator(Catalog);

        Assert.Equal((decimal)expected, calculator.Tax((decimal)amount));
    }

    [Fact]
    public void Tax_RoundsHalfCents_Up()
    {
        var calculator = new CartCalculator(Catalog);

        Assert.Equal(0.03m, calculator.Tax(0.125m));
    }
}

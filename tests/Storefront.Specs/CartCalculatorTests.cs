using Storefront.Checkout;
using Storefront.Pricing;

namespace Storefront.Specs;

public class CartCalculatorTests : StorefrontTestBase
{
    [Fact]
    public void CartCalculator_SumsLines_ForSubtotal()
    {
        var calculator = new CartCalculator(Catalog);

        var subtotal = calculator.Subtotal(new[] { new CartLine("MUG", 2), new CartLine("TEA", 4) });

        Assert.Equal(42.00m, subtotal);
    }

    [Fact]
    public void CartCalculator_ReturnsZero_ForEmptyCart()
    {
        var calculator = new CartCalculator(Catalog);

        Assert.Equal(0m, calculator.Subtotal(Array.Empty<CartLine>()));
    }

    [Fact]
    public void CartCalculator_SumsWeight_AcrossLines()
    {
        var calculator = new CartCalculator(Catalog);

        var weight = calculator.WeightKg(new[] { new CartLine("KETTLE", 1, 1.8m), new CartLine("TEA", 4, 0.1m) });

        Assert.Equal(2.2m, weight);
    }
}

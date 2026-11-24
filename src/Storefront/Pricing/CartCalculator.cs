using Storefront.Checkout;

namespace Storefront.Pricing;

public class CartCalculator
{
    public const decimal TaxRate = 0.20m;

    private readonly PriceCatalog _catalog;

    public CartCalculator(PriceCatalog catalog)
    {
        _catalog = catalog;
    }

    public decimal Subtotal(IEnumerable<CartLine> lines)
    {
        return lines.Sum(line => _catalog.PriceOf(line.Sku) * line.Quantity);
    }

    public decimal Tax(decimal amount)
    {
        return Math.Round(amount * TaxRate, 2, MidpointRounding.AwayFromZero);
    }

    public decimal WeightKg(IEnumerable<CartLine> lines)
    {
        return lines.Sum(line => line.WeightKg * line.Quantity);
    }
}

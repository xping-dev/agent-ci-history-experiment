using Storefront.Invoices;
using Storefront.Pricing;
using Storefront.Shipping;

namespace Storefront.Checkout;

public class CheckoutService
{
    private readonly PriceCatalog _catalog;
    private readonly CartCalculator _calculator;
    private readonly ShippingCalculator _shipping;
    private int _nextInvoiceNumber = 1;

    public CheckoutService(PriceCatalog catalog, ShippingCalculator shipping)
    {
        _catalog = catalog;
        _calculator = new CartCalculator(catalog);
        _shipping = shipping;
    }

    public Invoice Checkout(string customerEmail, IReadOnlyList<CartLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("Cart is empty.", nameof(lines));
        }

        var subtotal = _calculator.Subtotal(lines);
        var shipping = _shipping.Cost(_calculator.WeightKg(lines), subtotal);
        var tax = _calculator.Tax(subtotal);

        var invoiceLines = lines
            .Select(line => new InvoiceLine(line.Sku, line.Quantity, _catalog.PriceOf(line.Sku)))
            .ToList();

        return new Invoice($"INV-{_nextInvoiceNumber++:00000}", customerEmail, invoiceLines, tax, shipping);
    }
}

using Storefront.Invoices;
using Storefront.Pricing;
using Storefront.Shipping;

namespace Storefront.Checkout;

public class CheckoutService
{
    private readonly PriceCatalog _catalog;
    private readonly CartCalculator _calculator;
    private readonly ShippingCalculator _shipping;
    private readonly string _invoicePrefix;
    private int _nextInvoiceNumber = 1;

    /// <param name="invoicePrefix">Prefix of the invoice numbers, so each store has its own number range.</param>
    public CheckoutService(PriceCatalog catalog, ShippingCalculator shipping, string invoicePrefix = "INV")
    {
        if (string.IsNullOrWhiteSpace(invoicePrefix))
        {
            throw new ArgumentException("Invoice prefix is empty.", nameof(invoicePrefix));
        }

        _invoicePrefix = invoicePrefix.Trim().ToUpperInvariant();
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

        var number = $"{_invoicePrefix}-{_nextInvoiceNumber++:00000}";
        return new Invoice(number, customerEmail, invoiceLines, tax, shipping);
    }
}

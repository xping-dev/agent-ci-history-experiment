namespace Storefront.Invoices;

public record InvoiceLine(string Sku, int Quantity, decimal UnitPrice);

public record Invoice(string Number, string CustomerEmail, IReadOnlyList<InvoiceLine> Lines, decimal Tax, decimal Shipping)
{
    /// <summary>The customer's own reference for this order, e.g. a purchase order number. Optional.</summary>
    public string? CustomerReference { get; init; }

    public decimal Subtotal => Lines.Sum(line => line.UnitPrice * line.Quantity);

    public decimal Total => Subtotal + Tax + Shipping;
}

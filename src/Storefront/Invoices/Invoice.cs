namespace Storefront.Invoices;

public record InvoiceLine(string Sku, int Quantity, decimal UnitPrice);

public record Invoice(string Number, string CustomerEmail, IReadOnlyList<InvoiceLine> Lines, decimal Tax, decimal Shipping)
{
    /// <summary>A short message printed on the receipt when the order is a gift. Optional.</summary>
    public string? GiftMessage { get; init; }

    public decimal Subtotal => Lines.Sum(line => line.UnitPrice * line.Quantity);

    public decimal Total => Subtotal + Tax + Shipping;
}

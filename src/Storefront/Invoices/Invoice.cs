namespace Storefront.Invoices;

public record InvoiceLine(string Sku, int Quantity, decimal UnitPrice);

public record Invoice(string Number, string CustomerEmail, IReadOnlyList<InvoiceLine> Lines, decimal Tax, decimal Shipping)
{
    public decimal Subtotal => Lines.Sum(line => line.UnitPrice * line.Quantity);

    public decimal Total => Subtotal + Tax + Shipping;
}

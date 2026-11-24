namespace Storefront.Checkout;

public record CartLine(string Sku, int Quantity, decimal WeightKg = 0.5m);

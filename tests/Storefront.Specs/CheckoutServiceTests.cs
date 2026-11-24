using System.Collections.Concurrent;
using Storefront.Checkout;
using Storefront.Shipping;

namespace Storefront.Specs;

public class CheckoutServiceTests : StorefrontTestBase
{
    private const int ParallelCheckouts = 64;

    [Fact]
    public void Checkout_BuildsInvoice_WithTaxAndShipping()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator());

        var invoice = service.Checkout("ana@example.com", new[] { new CartLine("MUG", 2), new CartLine("TEA", 4) });

        Assert.Equal(42.00m, invoice.Subtotal);
        Assert.Equal(8.40m, invoice.Tax);
        Assert.Equal(7.90m, invoice.Shipping);
        Assert.Equal(58.30m, invoice.Total);
    }

    [Fact]
    public void Checkout_NumbersInvoices_InSequence()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator());
        var cart = new[] { new CartLine("TEA", 1) };

        var first = service.Checkout("ana@example.com", cart);
        var second = service.Checkout("ben@example.com", cart);

        Assert.Equal("INV-00001", first.Number);
        Assert.Equal("INV-00002", second.Number);
    }

    [Fact]
    public void Checkout_NumbersInvoicesUniquely_WhenCheckoutsRunInParallel()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator());
        var cart = new[] { new CartLine("TEA", 1) };
        var numbers = new ConcurrentBag<string>();

        Parallel.For(0, ParallelCheckouts, _ => numbers.Add(service.Checkout("ana@example.com", cart).Number));

        Assert.Equal(ParallelCheckouts, numbers.Distinct().Count());
    }

    [Fact]
    public void Checkout_UsesStorePrefix_InInvoiceNumbers()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator(), "ber");

        var invoice = service.Checkout("ana@example.com", new[] { new CartLine("TEA", 1) });

        Assert.Equal("BER-00001", invoice.Number);
    }

    [Fact]
    public void Checkout_RejectsEmptyInvoicePrefix()
    {
        Assert.Throws<ArgumentException>(() => new CheckoutService(Catalog, new ShippingCalculator(), " "));
    }

    [Fact]
    public void Checkout_ShipsFree_ForLargeOrder()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator());

        var invoice = service.Checkout("ana@example.com", new[] { new CartLine("KETTLE", 4, 1.8m) });

        Assert.Equal(0m, invoice.Shipping);
    }

    [Fact]
    public void Checkout_RejectsEmptyCart()
    {
        var service = new CheckoutService(Catalog, new ShippingCalculator());

        Assert.Throws<ArgumentException>(() => service.Checkout("ana@example.com", Array.Empty<CartLine>()));
    }
}

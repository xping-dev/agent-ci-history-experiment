using Storefront.Invoices;

namespace Storefront.Specs;

public class InvoiceTests : StorefrontTestBase
{
    [Fact]
    public void Invoice_Total_AddsTaxAndShipping()
    {
        var invoice = new Invoice("INV-00007", "ana@example.com", new[] { new InvoiceLine("MUG", 2, 12.50m) }, 5.00m, 4.90m);

        Assert.Equal(34.90m, invoice.Total);
    }

    [Fact]
    public void Invoice_Render_ListsEveryLine()
    {
        var invoice = new Invoice(
            "INV-00007",
            "ana@example.com",
            new[] { new InvoiceLine("MUG", 2, 12.50m), new InvoiceLine("TEA", 1, 4.25m) },
            5.85m,
            4.90m);

        var text = InvoiceExporter.Render(invoice);

        Assert.Contains("2 x MUG @ 12.50", text);
        Assert.Contains("1 x TEA @ 4.25", text);
        Assert.Contains("Total: 40.00", text);
    }
}

using System.Diagnostics;
using Storefront.Invoices;

namespace Storefront.Specs;

public class ReceiptTests
{
    private const int BatchSize = 60000;
    private static readonly TimeSpan BatchBudget = TimeSpan.FromMilliseconds(250);

    [Fact]
    public void Receipt_ListsEveryLine_AndTheOrderNumber()
    {
        var text = Receipt.Render(Order(7));

        Assert.StartsWith("Thank you for your order INV-00007.", text);
        Assert.Contains("2 x MUG", text);
        Assert.Contains("4 x TEA", text);
    }

    [Fact]
    public void Receipt_PrintsGiftMessage_WhenSet()
    {
        var order = Order(8) with { GiftMessage = "Happy birthday, Ana!" };

        Assert.EndsWith("Gift message:" + Environment.NewLine + "Happy birthday, Ana!" + Environment.NewLine, Receipt.Render(order));
    }

    [Fact]
    public void Receipt_HasNoGiftSection_WithoutMessage()
    {
        Assert.DoesNotContain("Gift message", Receipt.Render(Order(9)));
    }

    [Fact]
    public void Receipt_RendersBatch_WithinTimeBudget()
    {
        var orders = Enumerable.Range(1, BatchSize).Select(Order).ToList();

        // The mailer renders receipts on all cores, so the batch is rendered the same way here.
        var stopwatch = Stopwatch.StartNew();
        var rendered = orders.AsParallel().Select(Receipt.Render).Count(text => text.Length > 0);
        stopwatch.Stop();

        Assert.Equal(BatchSize, rendered);
        Assert.True(
            stopwatch.Elapsed < BatchBudget,
            $"Rendering {BatchSize} receipts took {stopwatch.ElapsedMilliseconds} ms, budget is {BatchBudget.TotalMilliseconds} ms.");
    }

    private static Invoice Order(int number)
    {
        return new Invoice(
            $"INV-{number:00000}",
            $"customer{number}@example.com",
            new[] { new InvoiceLine("MUG", 2, 12.50m), new InvoiceLine("TEA", 4, 4.25m), new InvoiceLine("KETTLE", 1, 39.00m) },
            16.20m,
            7.90m);
    }
}

using System.Diagnostics;
using Storefront.Invoices;

namespace Storefront.Specs;

public class InvoiceExporterTests : StorefrontTestBase, IDisposable
{
    private const int BatchSize = 1000;
    private static readonly TimeSpan BatchBudget = TimeSpan.FromMilliseconds(250);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "storefront-export-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void InvoiceExporter_WritesOneFile_PerInvoice()
    {
        var exported = new InvoiceExporter().ExportAll(Invoices(3), _directory);

        Assert.Equal(3, exported);
        Assert.Equal(3, Directory.GetFiles(_directory, "INV-*.txt").Length);
    }

    [Fact]
    public void InvoiceExporter_ExportsBatch_WithinTimeBudget()
    {
        var invoices = Invoices(BatchSize);
        var exporter = new InvoiceExporter();

        var stopwatch = Stopwatch.StartNew();
        exporter.ExportAll(invoices, _directory);
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < BatchBudget,
            $"Exporting {BatchSize} invoices took {stopwatch.ElapsedMilliseconds} ms, budget is {BatchBudget.TotalMilliseconds} ms.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static List<Invoice> Invoices(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Invoice(
                $"INV-{i:00000}",
                $"customer{i}@example.com",
                new[] { new InvoiceLine("MUG", 2, 12.50m), new InvoiceLine("TEA", 4, 4.25m) },
                8.40m,
                7.90m))
            .ToList();
    }
}

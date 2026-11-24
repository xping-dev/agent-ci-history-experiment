using System.Diagnostics;
using Storefront.Invoices;

namespace Storefront.Specs;

public class LedgerWriterTests : IDisposable
{
    private const int BatchSize = 3000;
    private static readonly TimeSpan BatchBudget = TimeSpan.FromMilliseconds(200);

    private readonly string _directory = Path.Combine(StagingRoot(), "storefront-ledger-" + Guid.NewGuid().ToString("N"));

    public LedgerWriterTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Ledger_HasHeader_AndOneLinePerInvoice()
    {
        var path = Path.Combine(_directory, "ledger.csv");

        var written = new LedgerWriter().Write(Invoices(3), path);

        var lines = File.ReadAllLines(path);
        Assert.Equal(3, written);
        Assert.Equal(4, lines.Length);
        Assert.Equal("number;customer;subtotal;tax;shipping;total", lines[0]);
        Assert.Equal("INV-00001;customer1@example.com;42.00;8.40;7.90;58.30", lines[1]);
    }

    [Fact]
    public void Ledger_WritesDailyBatch_WithinTimeBudget()
    {
        var invoices = Invoices(BatchSize);
        var writer = new LedgerWriter();
        var path = Path.Combine(_directory, "ledger.csv");

        var stopwatch = Stopwatch.StartNew();
        writer.Write(invoices, path);
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < BatchBudget,
            $"Writing the ledger for {BatchSize} invoices took {stopwatch.ElapsedMilliseconds} ms, budget is {BatchBudget.TotalMilliseconds} ms.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    // CI writes to the accounting team's staging share (LEDGER_STAGING_DIR CI/CD variable), like the nightly job does.
    // Without the variable the ledger goes to the temp directory.
    private static string StagingRoot()
    {
        var directory = Environment.GetEnvironmentVariable("LEDGER_STAGING_DIR");
        return string.IsNullOrWhiteSpace(directory) ? Path.GetTempPath() : directory;
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

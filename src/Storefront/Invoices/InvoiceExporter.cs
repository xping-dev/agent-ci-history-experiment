using System.Globalization;
using System.Text;

namespace Storefront.Invoices;

/// <summary>Writes invoices as text files for the accounting import, one file per invoice.</summary>
public class InvoiceExporter
{
    public int ExportAll(IEnumerable<Invoice> invoices, string directory)
    {
        Directory.CreateDirectory(directory);

        var count = 0;
        foreach (var invoice in invoices)
        {
            // Write to a temporary name first, so the accounting import never picks up a half-written file.
            var path = Path.Combine(directory, $"{invoice.Number}.txt");
            File.WriteAllText(path + ".tmp", Render(invoice));
            File.Move(path + ".tmp", path, overwrite: true);
            count++;
        }

        return count;
    }

    public static string Render(Invoice invoice)
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"Invoice {invoice.Number}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Customer: {invoice.CustomerEmail}");

        if (!string.IsNullOrWhiteSpace(invoice.CustomerReference))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"Your reference: {invoice.CustomerReference.Trim()}");
        }
        text.AppendLine();

        foreach (var line in invoice.Lines)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{line.Quantity} x {line.Sku} @ {line.UnitPrice:0.00}");
        }

        text.AppendLine();
        text.AppendLine(CultureInfo.InvariantCulture, $"Subtotal: {invoice.Subtotal:0.00}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Tax: {invoice.Tax:0.00}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Shipping: {invoice.Shipping:0.00}");
        text.AppendLine(CultureInfo.InvariantCulture, $"Total: {invoice.Total:0.00}");
        return text.ToString();
    }
}

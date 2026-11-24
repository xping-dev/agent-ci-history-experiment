using System.Globalization;

namespace Storefront.Invoices;

/// <summary>Writes the daily ledger for accounting: one CSV file, one line per invoice.</summary>
public class LedgerWriter
{
    private const string Header = "number;customer;subtotal;tax;shipping;total";

    public int Write(IEnumerable<Invoice> invoices, string path)
    {
        // Write as we go: the month-end ledger no longer has to fit in memory.
        File.WriteAllText(path, Header + Environment.NewLine);

        var count = 0;
        foreach (var invoice in invoices)
        {
            File.AppendAllText(path, Line(invoice) + Environment.NewLine);
            count++;
        }

        return count;
    }

    public static string Line(Invoice invoice)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{invoice.Number};{invoice.CustomerEmail};{invoice.Subtotal:0.00};{invoice.Tax:0.00};{invoice.Shipping:0.00};{invoice.Total:0.00}");
    }
}

using System.Globalization;
using System.Text;

namespace Storefront.Invoices;

/// <summary>Writes the daily ledger for accounting: one CSV file, one line per invoice.</summary>
public class LedgerWriter
{
    private const string Header = "number;customer;subtotal;tax;shipping;total";

    public int Write(IEnumerable<Invoice> invoices, string path)
    {
        var text = new StringBuilder();
        text.AppendLine(Header);

        var count = 0;
        foreach (var invoice in invoices)
        {
            text.AppendLine(Line(invoice));
            count++;
        }

        File.WriteAllText(path, text.ToString());
        return count;
    }

    public static string Line(Invoice invoice)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{invoice.Number};{invoice.CustomerEmail};{invoice.Subtotal:0.00};{invoice.Tax:0.00};{invoice.Shipping:0.00};{invoice.Total:0.00}");
    }
}

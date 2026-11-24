using System.Text;

namespace Storefront.Invoices;

/// <summary>The receipt text a customer gets by e-mail after checkout.</summary>
public static class Receipt
{
    public static string Render(Invoice invoice)
    {
        var text = new StringBuilder();
        text.AppendLine($"Thank you for your order {invoice.Number}.");
        text.AppendLine();

        foreach (var line in invoice.Lines)
        {
            text.AppendLine($"{line.Quantity} x {line.Sku}".PadRight(24) + Money.Format(line.UnitPrice * line.Quantity).PadLeft(14));
        }

        text.AppendLine(new string('-', 38));
        text.AppendLine("Subtotal".PadRight(24) + Money.Format(invoice.Subtotal).PadLeft(14));
        text.AppendLine("VAT".PadRight(24) + Money.Format(invoice.Tax).PadLeft(14));
        text.AppendLine("Shipping".PadRight(24) + Money.Format(invoice.Shipping).PadLeft(14));
        text.AppendLine("Total".PadRight(24) + Money.Format(invoice.Total).PadLeft(14));

        if (!string.IsNullOrWhiteSpace(invoice.GiftMessage))
        {
            text.AppendLine();
            text.AppendLine("Gift message:");
            text.AppendLine(invoice.GiftMessage.Trim());
        }

        return text.ToString();
    }
}

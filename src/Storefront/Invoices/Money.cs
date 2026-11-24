using System.Globalization;

namespace Storefront.Invoices;

/// <summary>Formats amounts the way customers see them on receipts and e-mails. The store sells in euro.</summary>
public static class Money
{
    private static readonly CultureInfo StoreCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string Format(decimal amount)
    {
        return amount.ToString("N2", StoreCulture) + " €";
    }
}

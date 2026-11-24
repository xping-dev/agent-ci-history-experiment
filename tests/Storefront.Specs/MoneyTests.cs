using Storefront.Invoices;

namespace Storefront.Specs;

public class MoneyTests
{
    [Fact]
    public void Money_FormatsAmount_InStoreCurrency()
    {
        // German format: dot for thousands, comma for cents, non-breaking space before the euro sign.
        Assert.Equal("1.234,50 €", Money.Format(1234.50m));
    }
}

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

    [Fact]
    public void Money_ShowsFree_ForZeroAmount()
    {
        Assert.Equal("Free", Money.FormatOrFree(0m));
    }

    [Fact]
    public void Money_FormatsAsUsual_ForNonZeroAmount()
    {
        Assert.Equal(Money.Format(4.90m), Money.FormatOrFree(4.90m));
    }
}

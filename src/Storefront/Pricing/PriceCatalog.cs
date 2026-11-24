namespace Storefront.Pricing;

public class PriceCatalog
{
    private readonly IReadOnlyDictionary<string, decimal> _listPrices;

    public PriceCatalog(IReadOnlyDictionary<string, decimal> listPrices)
    {
        _listPrices = listPrices;
    }

    public static PriceCatalog Default() => new(new Dictionary<string, decimal>
    {
        ["MUG"] = 12.50m,
        ["TEA"] = 4.25m,
        ["KETTLE"] = 39.00m,
        ["FILTER"] = 2.80m,
    });

    public decimal PriceOf(string sku)
    {
        return TryPriceOf(sku, out var price)
            ? price
            : throw new KeyNotFoundException($"Unknown SKU '{sku}'.");
    }

    /// <summary>Looks up a price without throwing, for SKUs that may have been retired.</summary>
    public bool TryPriceOf(string sku, out decimal price)
    {
        if (!_listPrices.ContainsKey(sku))
        {
            price = 0m;
            return false;
        }

        price = PriceCache.GetOrAdd(sku, () => ListPriceOf(sku));
        return true;
    }

    /// <summary>Starts a promotion: the SKU sells at a percentage off its list price.</summary>
    public void ApplyPercentOff(string sku, decimal percent)
    {
        if (percent is <= 0 or >= 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100.");
        }

        var promoPrice = Math.Round(ListPriceOf(sku) * (1 - percent / 100m), 2, MidpointRounding.AwayFromZero);
        PriceCache.Set(sku, promoPrice);
    }

    /// <summary>Ends a promotion: the SKU sells at its list price again.</summary>
    public void EndPromotion(string sku)
    {
        PriceCache.Remove(sku);
    }

    private decimal ListPriceOf(string sku)
    {
        return _listPrices.TryGetValue(sku, out var price)
            ? price
            : throw new KeyNotFoundException($"Unknown SKU '{sku}'.");
    }
}

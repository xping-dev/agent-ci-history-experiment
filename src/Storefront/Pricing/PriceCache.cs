namespace Storefront.Pricing;

/// <summary>
/// Process-wide cache of resolved prices, so repeated lookups skip the price list.
/// </summary>
public static class PriceCache
{
    private static readonly Dictionary<string, decimal> Prices = new();
    private static readonly object Gate = new();
    private static long _lookups;

    /// <summary>Number of lookups since the cache was last cleared. Feeds the cache hit-rate metric.</summary>
    public static long Lookups
    {
        get
        {
            lock (Gate)
            {
                return _lookups;
            }
        }
    }

    public static decimal GetOrAdd(string sku, Func<decimal> resolve)
    {
        lock (Gate)
        {
            _lookups++;

            if (!Prices.TryGetValue(sku, out var price))
            {
                price = resolve();
                Prices[sku] = price;
            }

            return price;
        }
    }

    public static void Set(string sku, decimal price)
    {
        lock (Gate)
        {
            Prices[sku] = price;
        }
    }

    public static void Remove(string sku)
    {
        lock (Gate)
        {
            Prices.Remove(sku);
        }
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Prices.Clear();
            _lookups = 0;
        }
    }
}

namespace Storefront.Shipping;

public class ShippingCalculator
{
    public const decimal FreeShippingFrom = 150.00m;

    /// <summary>Shipping cost for a parcel, by weight. Orders from 150.00 ship free.</summary>
    public decimal Cost(decimal weightKg, decimal orderSubtotal)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be positive.");
        }

        if (orderSubtotal >= FreeShippingFrom)
        {
            return 0m;
        }

        if (weightKg <= 1m)
        {
            return 4.90m;
        }

        if (weightKg <= 5m)
        {
            return 7.90m;
        }

        if (weightKg <= 20m)
        {
            return 14.90m;
        }

        return 29.90m;
    }
}

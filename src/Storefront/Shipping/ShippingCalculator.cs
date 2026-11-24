namespace Storefront.Shipping;

public enum ShippingSpeed
{
    Standard,
    Express,
}

public class ShippingCalculator
{
    public const decimal FreeShippingFrom = 150.00m;
    public const decimal ExpressSurcharge = 6.00m;

    private const decimal FreightRate = 29.90m;

    private static readonly (decimal MaxWeightKg, decimal Rate)[] Brackets =
    {
        (1m, 4.90m),
        (5m, 7.90m),
        (20m, 14.90m),
    };

    /// <summary>
    /// Shipping cost for a parcel, by weight. Standard orders from 150.00 ship free;
    /// express always pays the surcharge.
    /// </summary>
    public decimal Cost(decimal weightKg, decimal orderSubtotal, ShippingSpeed speed = ShippingSpeed.Standard)
    {
        if (weightKg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), "Weight must be positive.");
        }

        var surcharge = speed == ShippingSpeed.Express ? ExpressSurcharge : 0m;

        if (orderSubtotal >= FreeShippingFrom)
        {
            return surcharge;
        }

        return RateFor(weightKg) + surcharge;
    }

    private static decimal RateFor(decimal weightKg)
    {
        foreach (var (maxWeightKg, rate) in Brackets)
        {
            if (weightKg < maxWeightKg)
            {
                return rate;
            }
        }

        return FreightRate;
    }
}

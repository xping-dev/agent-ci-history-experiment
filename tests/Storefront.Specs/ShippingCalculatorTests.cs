using Storefront.Shipping;

namespace Storefront.Specs;

public class ShippingCalculatorTests : StorefrontTestBase
{
    private readonly ShippingCalculator _shipping = new();

    [Fact]
    public void Shipping_ChargesSmallRate_ForLightParcel()
    {
        Assert.Equal(4.90m, _shipping.Cost(0.4m, 20.00m));
    }

    [Fact]
    public void Shipping_ChargesMediumRate_ForThreeKilograms()
    {
        Assert.Equal(7.90m, _shipping.Cost(3m, 20.00m));
    }

    [Fact]
    public void Shipping_ChargesMediumRate_AtFiveKilograms()
    {
        Assert.Equal(7.90m, _shipping.Cost(5m, 20.00m));
    }

    [Fact]
    public void Shipping_ChargesLargeRate_ForTwelveKilograms()
    {
        Assert.Equal(14.90m, _shipping.Cost(12m, 20.00m));
    }

    [Fact]
    public void Shipping_ChargesFreightRate_AboveTwentyKilograms()
    {
        Assert.Equal(29.90m, _shipping.Cost(31m, 20.00m));
    }

    [Fact]
    public void Shipping_IsFree_FromOrderThreshold()
    {
        Assert.Equal(0m, _shipping.Cost(3m, 150.00m));
    }

    [Fact]
    public void Shipping_RejectsParcel_WithoutWeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _shipping.Cost(0m, 20.00m));
    }

    [Fact]
    public void Shipping_AddsSurcharge_ForExpress()
    {
        Assert.Equal(13.90m, _shipping.Cost(3m, 20.00m, ShippingSpeed.Express));
    }

    [Fact]
    public void Shipping_ChargesOnlySurcharge_ForExpressAboveOrderThreshold()
    {
        Assert.Equal(6.00m, _shipping.Cost(3m, 150.00m, ShippingSpeed.Express));
    }
}

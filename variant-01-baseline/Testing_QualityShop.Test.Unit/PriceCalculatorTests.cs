using Testing_QualityShop.Core;

namespace Testing_QualityShop.Test.Unit;

public sealed class PriceCalculatorTests
{
    private readonly PriceCalculator _calculator = new();

    [Test]
    public void Calculate_sums_line_values_for_subtotal()
    {
        var cart = TestData.CartWith((200m, 2), (150m, 1));

        var result = _calculator.Calculate(cart);

        Assert.That(result.Subtotal, Is.EqualTo(550m));
    }

    [Test]
    public void Calculate_applies_ten_percent_for_save10()
    {
        var cart = TestData.CartWith((500m, 2));
        cart.DiscountCode = "SAVE10";

        var result = _calculator.Calculate(cart);

        Assert.That(result.Discount, Is.EqualTo(100m));
    }

    [Test]
    public void Calculate_charges_standard_shipping_below_threshold()
    {
        var cart = TestData.CartWith((999.99m, 1));

        var result = _calculator.Calculate(cart);

        Assert.That(result.Shipping, Is.EqualTo(99m));
    }

    [Test]
    [Category("DefectDemonstration")]
    public void Calculate_gives_free_shipping_at_exact_threshold()
    {
        var cart = TestData.CartWith((1000m, 1));

        var result = _calculator.Calculate(cart);

        Assert.That(result.Shipping, Is.Zero);
    }

    [Test]
    [Category("DefectDemonstration")]
    public void Calculate_accepts_lowercase_discount_code()
    {
        var cart = TestData.CartWith((1000m, 1));
        cart.DiscountCode = "save10";

        var result = _calculator.Calculate(cart);

        Assert.That(result.Discount, Is.EqualTo(100m));
    }
}

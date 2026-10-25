using Testing_QualityShop.DependencyInjection.Core;

namespace Testing_QualityShop.DependencyInjection.Test.Unit;

public sealed class PricingTests
{
    [TestCase(999.99, 99)]
    [TestCase(1000, 0)]
    [TestCase(1000.01, 0)]
    public void Shipping_uses_exact_discounted_boundary(decimal value, decimal expectedShipping)
    {
        var cart = new Cart();
        cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Boundary product", UnitPrice = value, Quantity = 1 });

        var totals = new PriceCalculator().Calculate(cart);

        Assert.That(totals.Shipping, Is.EqualTo(expectedShipping));
    }

    [TestCase("SAVE10")]
    [TestCase("save10")]
    [TestCase("Save10")]
    public void Discount_is_case_insensitive_and_applied_before_shipping(string code)
    {
        var cart = new Cart { DiscountCode = code };
        cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Discount product", UnitPrice = 1000m, Quantity = 1 });

        var totals = new PriceCalculator().Calculate(cart);

        Assert.That(totals, Is.EqualTo(new CartTotals(1000m, 100m, 99m, 999m)));
    }
}

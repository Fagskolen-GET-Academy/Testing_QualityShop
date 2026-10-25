using Testing_QualityShop.FunctionalCore.Core;

namespace Testing_QualityShop.FunctionalCore.Test.Unit;

public sealed class ShopRulesTests
{
    [TestCase("999.99", "99")]
    [TestCase("1000.00", "0")]
    [TestCase("1000.01", "0")]
    public void Shipping_uses_the_exact_boundary(string value, string expected)
    {
        var cart = CartWith(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture));

        var totals = ShopRules.Calculate(cart);

        Assert.That(totals.Shipping, Is.EqualTo(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [TestCase("SAVE10")]
    [TestCase("save10")]
    [TestCase("Save10")]
    public void Discount_is_case_insensitive_and_shipping_uses_discounted_value(string code)
    {
        var cart = CartWith(1000m);
        cart.DiscountCode = code;

        var totals = ShopRules.Calculate(cart);

        Assert.That(totals, Is.EqualTo(new CartTotals(1000m, 100m, 99m, 999m)));
    }

    [Test]
    public void Subtotal_sums_price_times_quantity()
    {
        var cart = CartWith(200m, 2);
        cart.Items.Add(new CartItem { ProductId = 2, ProductName = "Second", UnitPrice = 150m, Quantity = 1 });

        Assert.That(ShopRules.Calculate(cart).Subtotal, Is.EqualTo(550m));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void Empty_discount_means_no_discount(string? code) =>
        Assert.That(ShopRules.CalculateDiscount(500m, code), Is.Zero);

    [Test]
    public void Unknown_discount_is_rejected() =>
        Assert.Throws<ShopValidationException>(() => ShopRules.CalculateDiscount(500m, "OTHER"));

    [Test]
    public void Negative_value_cannot_produce_a_negative_discount_or_total()
    {
        var cart = CartWith(-100m);
        cart.DiscountCode = "SAVE10";

        var totals = ShopRules.Calculate(cart);

        Assert.Multiple(() =>
        {
            Assert.That(totals.Discount, Is.Zero);
            Assert.That(totals.Total, Is.GreaterThanOrEqualTo(0m));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Quantity_must_be_positive(int quantity) =>
        Assert.Throws<ShopValidationException>(() => ShopRules.ChangeQuantity(new Cart(), Product(), quantity, add: true));

    [Test]
    public void Repeated_add_checks_combined_quantity_without_mutating_input()
    {
        var cart = CartWith(100m, 3);

        Assert.Throws<ShopConflictException>(() => ShopRules.ChangeQuantity(cart, Product(), 3, add: true));
        Assert.That(cart.Items.Single().Quantity, Is.EqualTo(3));
    }

    [Test]
    public void Adding_at_stock_limit_returns_a_new_cart()
    {
        var cart = CartWith(100m, 3);

        var next = ShopRules.ChangeQuantity(cart, Product(), 2, add: true);

        Assert.Multiple(() =>
        {
            Assert.That(next.Items.Single().Quantity, Is.EqualTo(5));
            Assert.That(cart.Items.Single().Quantity, Is.EqualTo(3));
        });
    }

    [Test]
    public void Updating_replaces_quantity_instead_of_adding()
    {
        var next = ShopRules.ChangeQuantity(CartWith(100m, 3), Product(), 2, add: false);

        Assert.That(next.Items.Single().Quantity, Is.EqualTo(2));
    }

    [Test]
    public void Empty_cart_cannot_be_ordered() =>
        Assert.Throws<ShopValidationException>(() => ShopRules.PrepareOrder(new Cart(), [Product()], 1, DateTime.UnixEpoch));

    [Test]
    public void Order_plan_contains_stock_changes_and_supplied_time_without_changing_inputs()
    {
        var product = Product();
        var cart = CartWith(100m, 2);

        var plan = ShopRules.PrepareOrder(cart, [product], 42, DateTime.UnixEpoch);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Order.Id, Is.EqualTo(42));
            Assert.That(plan.Order.CreatedAt, Is.EqualTo(DateTime.UnixEpoch));
            Assert.That(plan.Order.Total, Is.EqualTo(299m));
            Assert.That(plan.StockChanges, Is.EqualTo(new[] { new StockChange(1, 3) }));
            Assert.That(product.StockQuantity, Is.EqualTo(5));
            Assert.That(cart.Items.Single().Quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public void Failed_order_leaves_cart_and_stock_unchanged()
    {
        var cart = CartWith(100m, 6);
        var product = Product();

        Assert.Throws<ShopConflictException>(() => ShopRules.PrepareOrder(cart, [product], 1, DateTime.UnixEpoch));
        Assert.Multiple(() =>
        {
            Assert.That(cart.Items.Single().Quantity, Is.EqualTo(6));
            Assert.That(product.StockQuantity, Is.EqualTo(5));
        });
    }

    private static Product Product() => new() { Id = 1, Name = "Test product", Price = 100m, StockQuantity = 5 };

    private static Cart CartWith(decimal price, int quantity = 1)
    {
        var cart = new Cart();
        cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Test product", UnitPrice = price, Quantity = quantity });
        return cart;
    }
}

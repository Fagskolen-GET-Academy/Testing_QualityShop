using Testing_QualityShop.DependencyInjection.Core;

namespace Testing_QualityShop.DependencyInjection.Test.Unit;

public sealed class ShopServiceTests
{
    private FakeShopRepository _repository = null!;
    private ShopService _shop = null!;

    [SetUp]
    public void Arrange()
    {
        _repository = new FakeShopRepository();
        _shop = new ShopService(_repository, new PriceCalculator(), new FixedClock());
    }

    [Test]
    public void Successful_order_saves_lines_totals_stock_and_empty_cart()
    {
        _shop.AddToCart(2, 2);
        _shop.ApplyDiscount("save10");

        var order = _shop.PlaceOrder();

        Assert.Multiple(() =>
        {
            Assert.That(order.CreatedAt, Is.EqualTo(DateTime.UnixEpoch));
            Assert.That(order.Total, Is.EqualTo(817.2m));
            Assert.That(_repository.Saved.Orders.Single().Lines.Single().Quantity, Is.EqualTo(2));
            Assert.That(_repository.Saved.Products.Single(p => p.Id == 2).StockQuantity, Is.EqualTo(10));
            Assert.That(_repository.Saved.Cart.Items, Is.Empty);
            Assert.That(_repository.Saved.Cart.DiscountCode, Is.Null);
        });
    }

    [Test]
    public void Stock_changed_since_arrange_rejects_order_without_saving_partial_changes()
    {
        _shop.AddToCart(1, 3);
        _repository.Saved.Products.Single(p => p.Id == 1).StockQuantity = 2;
        var previousCommits = _repository.CommitCount;

        Assert.Throws<ShopConflictException>(() => _shop.PlaceOrder());

        Assert.Multiple(() =>
        {
            Assert.That(_repository.Saved.Orders, Is.Empty);
            Assert.That(_repository.Saved.Cart.Items.Single().Quantity, Is.EqualTo(3));
            Assert.That(_repository.Saved.Products.Single(p => p.Id == 1).StockQuantity, Is.EqualTo(2));
            Assert.That(_repository.CommitCount, Is.EqualTo(previousCommits));
        });
    }

    [Test]
    public void Repeated_add_cannot_exceed_combined_stock()
    {
        _shop.AddToCart(1, 3);

        Assert.Throws<ShopConflictException>(() => _shop.AddToCart(1, 3));
        Assert.That(_shop.GetCart().Items.Single().Quantity, Is.EqualTo(3));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Invalid_quantity_is_rejected_before_commit(int quantity)
    {
        Assert.Throws<ShopValidationException>(() => _shop.AddToCart(1, quantity));
        Assert.That(_repository.CommitCount, Is.Zero);
    }

    [Test]
    public void Unknown_product_is_rejected_before_commit()
    {
        Assert.Throws<ShopNotFoundException>(() => _shop.AddToCart(999, 1));
        Assert.That(_repository.CommitCount, Is.Zero);
    }

    [Test]
    public void Empty_order_is_rejected() => Assert.Throws<ShopValidationException>(() => _shop.PlaceOrder());

    [Test]
    public void Invalid_discount_does_not_replace_previous_valid_discount()
    {
        _shop.ApplyDiscount("SAVE10");

        Assert.Throws<ShopValidationException>(() => _shop.ApplyDiscount("OTHER"));
        Assert.That(_shop.GetCart().DiscountCode, Is.EqualTo("SAVE10"));
    }

    [Test]
    public void Detached_read_cannot_change_persisted_stock()
    {
        _shop.GetProduct(1)!.StockQuantity = 0;

        Assert.That(_shop.GetProduct(1)!.StockQuantity, Is.EqualTo(5));
    }

    // This assertion observes an interaction (a spy), rather than only saved state.
    [Test]
    public void One_order_commits_once_after_all_changes_are_prepared()
    {
        _shop.AddToCart(3, 1);
        var before = _repository.CommitCount;

        _shop.PlaceOrder();

        Assert.That(_repository.CommitCount - before, Is.EqualTo(1));
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch;
    }
}

namespace Testing_QualityShop.Test.Unit;

public sealed class ExistingPricingTests
{
    [Test]
    public void Test1()
    {
        var store = new Core.InMemoryShopStore();
        var calculator = new Core.PriceCalculator();
        var service = new Core.ShopService(store, calculator);
        service.AddToCart(2, 1);
        var result = service.GetCartTotals();
        Assert.That(result.Total, Is.EqualTo(498m));
    }

    [Test]
    public void Test2()
    {
        var store = new Core.InMemoryShopStore();
        var calculator = new Core.PriceCalculator();
        var service = new Core.ShopService(store, calculator);
        service.AddToCart(2, 2);
        var result = service.GetCartTotals();
        Assert.That(result.Total, Is.EqualTo(897m));
    }

    [Test]
    public void Test3()
    {
        var store = new Core.InMemoryShopStore();
        var calculator = new Core.PriceCalculator();
        var service = new Core.ShopService(store, calculator);
        service.AddToCart(5, 1);
        var result = service.GetCartTotals();
        Assert.Multiple(() =>
        {
            Assert.That(result.Subtotal, Is.EqualTo(599m));
            Assert.That(result.Discount, Is.Zero);
            Assert.That(result.Shipping, Is.EqualTo(99m));
            Assert.That(result.Total, Is.EqualTo(698m));
            Assert.That(service.GetCart().Items[0].Quantity, Is.EqualTo(1));
        });
    }
}

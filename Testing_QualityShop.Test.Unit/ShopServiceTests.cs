namespace Testing_QualityShop.Test.Unit;

public sealed class ShopServiceTests
{
    [Test]
    public void AddToCart_adds_the_selected_product()
    {
        var shop = TestData.CreateService();

        shop.AddToCart(productId: 1, quantity: 2);

        var item = shop.GetCart().Items.Single();
        Assert.Multiple(() =>
        {
            Assert.That(item.ProductName, Is.EqualTo("Mechanical Keyboard"));
            Assert.That(item.Quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlaceOrder_creates_order_and_clears_cart()
    {
        var shop = TestData.CreateService();
        shop.AddToCart(productId: 2, quantity: 1);

        var order = shop.PlaceOrder();

        Assert.Multiple(() =>
        {
            Assert.That(order.Id, Is.EqualTo(1));
            Assert.That(order.Lines, Has.Count.EqualTo(1));
            Assert.That(order.Lines[0].ProductName, Is.EqualTo("Wireless Mouse"));
            Assert.That(shop.GetCart().Items, Is.Empty);
        });
    }
}

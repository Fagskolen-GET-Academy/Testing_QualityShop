namespace Testing_QualityShop.Test.Unit;

public sealed class OrderTimestampTests
{
    [Test]
    [Explicit("Run this timing example separately.")]
    [Category("FlakyExample")]
    public void New_order_uses_the_current_time()
    {
        var shop = TestData.CreateService();
        shop.AddToCart(3, 1);

        var order = shop.PlaceOrder();

        Assert.That((DateTime.UtcNow - order.CreatedAt).TotalMilliseconds, Is.LessThan(2));
    }
}

using Testing_QualityShop.Core;

namespace Testing_QualityShop.Test.Unit;

internal static class TestData
{
    public static Cart CartWith(params (decimal UnitPrice, int Quantity)[] items)
    {
        var cart = new Cart();
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            cart.Items.Add(new CartItem
            {
                ProductId = index + 1,
                ProductName = $"Product {index + 1}",
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
            });
        }

        return cart;
    }

    public static ShopService CreateService() =>
        new(new InMemoryShopStore(), new PriceCalculator());
}

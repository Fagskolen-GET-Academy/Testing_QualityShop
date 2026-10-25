namespace Testing_QualityShop.SqlServer.Core;

public interface IShopRepository
{
    IReadOnlyList<Product> GetProducts();

    Product? GetProduct(int id);

    IReadOnlyList<Product> SearchProducts(string? query);

    Cart GetCart();

    IReadOnlyList<Order> GetOrders();

    Order? GetOrder(int id);

    // Only mutations need the locked working snapshot and explicit commit.
    IShopSession OpenSession();

    void Reset();
}

// A session owns a consistent snapshot. Disposing without Commit discards changes.
// Order, stock and cart changes must commit together, including with a real database.
public interface IShopSession : IDisposable
{
    ShopState State { get; }

    void Commit();
}

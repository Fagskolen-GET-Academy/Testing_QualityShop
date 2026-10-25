namespace Testing_QualityShop.DependencyInjection.Core;

public interface IShopRepository
{
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

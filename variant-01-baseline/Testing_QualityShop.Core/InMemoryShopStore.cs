namespace Testing_QualityShop.Core;

public sealed class InMemoryShopStore
{
    private readonly List<Product> _products = [];
    private readonly List<Order> _orders = [];
    private int _nextOrderId;

    public InMemoryShopStore()
    {
        Reset();
    }

    internal object SyncRoot { get; } = new();

    internal List<Product> Products => _products;

    internal Cart Cart { get; } = new();

    internal List<Order> Orders => _orders;

    internal int NextOrderId() => _nextOrderId++;

    public void Reset()
    {
        lock (SyncRoot)
        {
            _products.Clear();
            _products.AddRange(
            [
                new Product { Id = 1, Name = "Mechanical Keyboard", Price = 899m, StockQuantity = 5 },
                new Product { Id = 2, Name = "Wireless Mouse", Price = 399m, StockQuantity = 12 },
                new Product { Id = 3, Name = "USB-C Hub", Price = 499m, StockQuantity = 8 },
                new Product { Id = 4, Name = "Webcam", Price = 749m, StockQuantity = 4 },
                new Product { Id = 5, Name = "Laptop Stand", Price = 599m, StockQuantity = 10 },
                new Product { Id = 6, Name = "Headset", Price = 999m, StockQuantity = 3 },
            ]);

            Cart.Items.Clear();
            Cart.DiscountCode = null;
            _orders.Clear();
            _nextOrderId = 1;
        }
    }
}

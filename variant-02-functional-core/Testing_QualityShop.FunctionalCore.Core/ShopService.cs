namespace Testing_QualityShop.FunctionalCore.Core;

public sealed class ShopService(InMemoryShopStore store)
{
    public IReadOnlyList<Product> GetProducts()
    {
        lock (store.SyncRoot)
        {
            return store.Products.ToList();
        }
    }

    public Product? GetProduct(int id)
    {
        lock (store.SyncRoot)
        {
            return store.Products.SingleOrDefault(product => product.Id == id);
        }
    }

    public IReadOnlyList<Product> SearchProducts(string? query)
    {
        lock (store.SyncRoot)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return store.Products.ToList();
            }

            return store.Products
                .Where(product => product.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public Cart GetCart() => store.Cart;

    public CartTotals GetCartTotals()
    {
        lock (store.SyncRoot)
        {
            return ShopRules.Calculate(store.Cart);
        }
    }

    public void AddToCart(int productId, int quantity)
    {
        lock (store.SyncRoot)
        {
            ReplaceCart(ShopRules.ChangeQuantity(store.Cart, FindProduct(productId), quantity, add: true));
        }
    }

    public void UpdateCartItem(int productId, int quantity)
    {
        lock (store.SyncRoot)
        {
            ReplaceCart(ShopRules.ChangeQuantity(store.Cart, FindProduct(productId), quantity, add: false));
        }
    }

    public void RemoveCartItem(int productId)
    {
        lock (store.SyncRoot)
        {
            var item = store.Cart.Items.SingleOrDefault(cartItem => cartItem.ProductId == productId)
                ?? throw new ShopNotFoundException("The product is not in the cart.");
            store.Cart.Items.Remove(item);
        }
    }

    public void ClearCart()
    {
        lock (store.SyncRoot)
        {
            store.Cart.Items.Clear();
            store.Cart.DiscountCode = null;
        }
    }

    public void ApplyDiscount(string? code)
    {
        lock (store.SyncRoot)
        {
            _ = ShopRules.CalculateDiscount(
                store.Cart.Items.Sum(item => item.LineTotal),
                code);
            store.Cart.DiscountCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        }
    }

    public void RemoveDiscount()
    {
        lock (store.SyncRoot)
        {
            store.Cart.DiscountCode = null;
        }
    }

    public Order PlaceOrder()
    {
        lock (store.SyncRoot)
        {
            // The shell supplies time and identity; the core only computes a plan.
            var plan = ShopRules.PrepareOrder(store.Cart, store.Products, store.Orders.Count + 1, DateTime.UtcNow);
            foreach (var change in plan.StockChanges)
            {
                FindProduct(change.ProductId).StockQuantity = change.RemainingQuantity;
            }

            store.Orders.Add(plan.Order);
            store.Cart.Items.Clear();
            store.Cart.DiscountCode = null;
            return plan.Order;
        }
    }

    public IReadOnlyList<Order> GetOrders()
    {
        lock (store.SyncRoot)
        {
            return store.Orders.ToList();
        }
    }

    public Order? GetOrder(int id)
    {
        lock (store.SyncRoot)
        {
            return store.Orders.SingleOrDefault(order => order.Id == id);
        }
    }

    private Product FindProduct(int productId) =>
        store.Products.SingleOrDefault(product => product.Id == productId)
        ?? throw new ShopNotFoundException("Product not found.");

    private void ReplaceCart(Cart next)
    {
        store.Cart.Items.Clear();
        store.Cart.Items.AddRange(next.Items);
        store.Cart.DiscountCode = next.DiscountCode;
    }
}

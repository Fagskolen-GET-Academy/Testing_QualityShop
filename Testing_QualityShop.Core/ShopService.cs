namespace Testing_QualityShop.Core;

public sealed class ShopService(InMemoryShopStore store, PriceCalculator priceCalculator)
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
            return priceCalculator.Calculate(store.Cart);
        }
    }

    public void AddToCart(int productId, int quantity)
    {
        lock (store.SyncRoot)
        {
            var product = FindProduct(productId);
            ValidateQuantity(quantity);

            if (quantity > product.StockQuantity)
            {
                throw new ShopConflictException("Requested quantity exceeds available stock.");
            }

            var existingItem = store.Cart.Items.SingleOrDefault(item => item.ProductId == productId);
            if (existingItem is not null)
            {
                existingItem.Quantity += quantity;
                return;
            }

            store.Cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity,
            });
        }
    }

    public void UpdateCartItem(int productId, int quantity)
    {
        lock (store.SyncRoot)
        {
            ValidateQuantity(quantity);
            var product = FindProduct(productId);
            var item = store.Cart.Items.SingleOrDefault(cartItem => cartItem.ProductId == productId)
                ?? throw new ShopNotFoundException("The product is not in the cart.");

            if (quantity > product.StockQuantity)
            {
                throw new ShopConflictException("Requested quantity exceeds available stock.");
            }

            item.Quantity = quantity;
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
            _ = priceCalculator.CalculateDiscount(
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
            if (store.Cart.Items.Count == 0)
            {
                throw new ShopValidationException("The cart is empty.");
            }

            foreach (var item in store.Cart.Items)
            {
                var product = FindProduct(item.ProductId);
                if (item.Quantity > product.StockQuantity)
                {
                    throw new ShopConflictException($"Insufficient stock for {product.Name}.");
                }
            }

            var totals = priceCalculator.Calculate(store.Cart);
            var order = new Order
            {
                Id = store.NextOrderId(),
                CreatedAt = DateTime.UtcNow,
                Lines = store.Cart.Items.Select(item => new OrderLine
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                }).ToList(),
                Subtotal = totals.Subtotal,
                Discount = totals.Discount,
                Shipping = totals.Shipping,
                Total = totals.Total,
            };

            foreach (var item in store.Cart.Items)
            {
                FindProduct(item.ProductId).StockQuantity -= item.Quantity;
            }

            store.Orders.Add(order);
            store.Cart.Items.Clear();
            store.Cart.DiscountCode = null;
            return order;
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

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < 1)
        {
            throw new ShopValidationException("Quantity must be at least 1.");
        }
    }
}

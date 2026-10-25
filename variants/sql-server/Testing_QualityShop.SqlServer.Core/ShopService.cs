namespace Testing_QualityShop.SqlServer.Core;

public sealed class ShopService(IShopRepository repository, PriceCalculator prices, TimeProvider clock)
{
    public IReadOnlyList<Product> GetProducts()
    {
        using var session = repository.OpenSession();
        return session.State.Products.ToList();
    }

    public Product? GetProduct(int id) => GetProducts().SingleOrDefault(p => p.Id == id);

    public IReadOnlyList<Product> SearchProducts(string? query) => GetProducts()
        .Where(p => string.IsNullOrWhiteSpace(query) || p.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
        .ToList();

    public Cart GetCart()
    {
        using var session = repository.OpenSession();
        return session.State.Cart;
    }

    public CartTotals GetCartTotals() => prices.Calculate(GetCart());

    public void AddToCart(int productId, int quantity)
    {
        using var session = repository.OpenSession();
        var state = session.State;
        var product = FindProduct(state, productId);
        ValidateQuantity(quantity);
        var item = state.Cart.Items.SingleOrDefault(i => i.ProductId == productId);
        var combined = (long)quantity + (item?.Quantity ?? 0);
        if (combined > product.StockQuantity)
        {
            throw new ShopConflictException("Requested quantity exceeds available stock.");
        }

        if (item is null)
        {
            state.Cart.Items.Add(new CartItem
            {
                ProductId = product.Id, ProductName = product.Name, UnitPrice = product.Price, Quantity = quantity,
            });
        }
        else
        {
            item.Quantity = (int)combined;
        }

        session.Commit();
    }

    public void UpdateCartItem(int productId, int quantity)
    {
        using var session = repository.OpenSession();
        ValidateQuantity(quantity);
        var product = FindProduct(session.State, productId);
        var item = session.State.Cart.Items.SingleOrDefault(i => i.ProductId == productId)
            ?? throw new ShopNotFoundException("The product is not in the cart.");
        if (quantity > product.StockQuantity)
        {
            throw new ShopConflictException("Requested quantity exceeds available stock.");
        }

        item.Quantity = quantity;
        session.Commit();
    }

    public void RemoveCartItem(int productId)
    {
        using var session = repository.OpenSession();
        var item = session.State.Cart.Items.SingleOrDefault(i => i.ProductId == productId)
            ?? throw new ShopNotFoundException("The product is not in the cart.");
        session.State.Cart.Items.Remove(item);
        session.Commit();
    }

    public void ClearCart()
    {
        using var session = repository.OpenSession();
        session.State.Cart.Items.Clear();
        session.State.Cart.DiscountCode = null;
        session.Commit();
    }

    public void ApplyDiscount(string? code)
    {
        using var session = repository.OpenSession();
        _ = prices.CalculateDiscount(session.State.Cart.Items.Sum(i => i.LineTotal), code);
        session.State.Cart.DiscountCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        session.Commit();
    }

    public void RemoveDiscount() => ApplyDiscount(null);

    public Order PlaceOrder()
    {
        using var session = repository.OpenSession();
        var state = session.State;
        if (state.Cart.Items.Count == 0)
        {
            throw new ShopValidationException("The cart is empty.");
        }

        foreach (var item in state.Cart.Items)
        {
            ValidateQuantity(item.Quantity);
            if (item.Quantity > FindProduct(state, item.ProductId).StockQuantity)
            {
                throw new ShopConflictException($"Insufficient stock for {item.ProductName}.");
            }
        }

        var totals = prices.Calculate(state.Cart);
        var order = new Order
        {
            Id = state.NextOrderId++,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            Lines = state.Cart.Items.Select(i => new OrderLine
            {
                ProductId = i.ProductId, ProductName = i.ProductName, UnitPrice = i.UnitPrice, Quantity = i.Quantity,
            }).ToList(),
            Subtotal = totals.Subtotal, Discount = totals.Discount, Shipping = totals.Shipping, Total = totals.Total,
        };
        foreach (var item in state.Cart.Items)
        {
            FindProduct(state, item.ProductId).StockQuantity -= item.Quantity;
        }

        state.Orders.Add(order);
        state.Cart.Items.Clear();
        state.Cart.DiscountCode = null;
        session.Commit();
        return order;
    }

    public IReadOnlyList<Order> GetOrders()
    {
        using var session = repository.OpenSession();
        return session.State.Orders.ToList();
    }

    public Order? GetOrder(int id) => GetOrders().SingleOrDefault(o => o.Id == id);

    private static Product FindProduct(ShopState state, int id) => state.Products.SingleOrDefault(p => p.Id == id)
        ?? throw new ShopNotFoundException("Product not found.");

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < 1)
        {
            throw new ShopValidationException("Quantity must be at least 1.");
        }
    }
}

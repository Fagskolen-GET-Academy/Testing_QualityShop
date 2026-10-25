namespace Testing_QualityShop.FunctionalCore.Core;

// Input objects belong to the caller. These rules never change them and perform no IO.
public static partial class ShopRules
{
    public static Cart ChangeQuantity(Cart cart, Product product, int quantity, bool add)
    {
        ValidateQuantity(quantity);
        var existing = cart.Items.SingleOrDefault(item => item.ProductId == product.Id);
        if (!add && existing is null)
        {
            throw new ShopNotFoundException("The product is not in the cart.");
        }

        var combined = (long)quantity + (add ? existing?.Quantity ?? 0 : 0);
        if (combined > product.StockQuantity)
        {
            throw new ShopConflictException("Requested quantity exceeds available stock.");
        }

        var next = new Cart { DiscountCode = cart.DiscountCode };
        next.Items.AddRange(cart.Items.Where(item => item.ProductId != product.Id).Select(CopyItem));
        next.Items.Add(new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = (int)combined,
        });
        return next;
    }

    public static OrderPlan PrepareOrder(Cart cart, IReadOnlyList<Product> products, int id, DateTime createdAt)
    {
        if (cart.Items.Count == 0)
        {
            throw new ShopValidationException("The cart is empty.");
        }

        var changes = new List<StockChange>();
        foreach (var item in cart.Items)
        {
            ValidateQuantity(item.Quantity);
            var product = products.SingleOrDefault(p => p.Id == item.ProductId)
                ?? throw new ShopNotFoundException("Product not found.");
            if (item.Quantity > product.StockQuantity)
            {
                throw new ShopConflictException($"Insufficient stock for {product.Name}.");
            }

            changes.Add(new StockChange(product.Id, product.StockQuantity - item.Quantity));
        }

        var totals = Calculate(cart);
        var order = new Order
        {
            Id = id,
            CreatedAt = createdAt,
            Lines = cart.Items.Select(item => new OrderLine
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
        return new OrderPlan(order, changes);
    }

    public static void ValidateQuantity(int quantity)
    {
        if (quantity < 1)
        {
            throw new ShopValidationException("Quantity must be at least 1.");
        }
    }

    private static CartItem CopyItem(CartItem item) => new()
    {
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
    };
}

public sealed record StockChange(int ProductId, int RemainingQuantity);

public sealed record OrderPlan(Order Order, IReadOnlyList<StockChange> StockChanges);

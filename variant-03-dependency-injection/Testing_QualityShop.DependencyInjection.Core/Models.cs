namespace Testing_QualityShop.DependencyInjection.Core;

public sealed class Product
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required decimal Price { get; init; }

    public int StockQuantity { get; set; }
}

public sealed class CartItem
{
    public required int ProductId { get; init; }

    public required string ProductName { get; init; }

    public required decimal UnitPrice { get; init; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Cart
{
    public List<CartItem> Items { get; } = [];

    public string? DiscountCode { get; set; }
}

public sealed class OrderLine
{
    public required int ProductId { get; init; }

    public required string ProductName { get; init; }

    public required decimal UnitPrice { get; init; }

    public required int Quantity { get; init; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class Order
{
    public required int Id { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required List<OrderLine> Lines { get; init; }

    public required decimal Subtotal { get; init; }

    public required decimal Discount { get; init; }

    public required decimal Shipping { get; init; }

    public required decimal Total { get; init; }
}

public sealed record CartTotals(
    decimal Subtotal,
    decimal Discount,
    decimal Shipping,
    decimal Total);

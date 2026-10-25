namespace Testing_QualityShop.DependencyInjection.Core;

public sealed class ShopState
{
    public List<Product> Products { get; } = [];

    public Cart Cart { get; } = new();

    public List<Order> Orders { get; } = [];

    public int NextOrderId { get; set; } = 1;

    public static ShopState Seed()
    {
        var state = new ShopState();
        state.Products.AddRange(
        [
            new Product { Id = 1, Name = "Mechanical Keyboard", Price = 899m, StockQuantity = 5 },
            new Product { Id = 2, Name = "Wireless Mouse", Price = 399m, StockQuantity = 12 },
            new Product { Id = 3, Name = "USB-C Hub", Price = 499m, StockQuantity = 8 },
            new Product { Id = 4, Name = "Webcam", Price = 749m, StockQuantity = 4 },
            new Product { Id = 5, Name = "Laptop Stand", Price = 599m, StockQuantity = 10 },
            new Product { Id = 6, Name = "Headset", Price = 999m, StockQuantity = 3 },
        ]);
        return state;
    }

    public ShopState Copy()
    {
        var copy = new ShopState { NextOrderId = NextOrderId };
        copy.Products.AddRange(Products.Select(p => new Product
        {
            Id = p.Id, Name = p.Name, Price = p.Price, StockQuantity = p.StockQuantity,
        }));
        copy.Cart.DiscountCode = Cart.DiscountCode;
        copy.Cart.Items.AddRange(Cart.Items.Select(i => new CartItem
        {
            ProductId = i.ProductId, ProductName = i.ProductName, UnitPrice = i.UnitPrice, Quantity = i.Quantity,
        }));
        copy.Orders.AddRange(Orders.Select(o => new Order
        {
            Id = o.Id, CreatedAt = o.CreatedAt, Subtotal = o.Subtotal, Discount = o.Discount,
            Shipping = o.Shipping, Total = o.Total,
            Lines = o.Lines.Select(i => new OrderLine
            {
                ProductId = i.ProductId, ProductName = i.ProductName, UnitPrice = i.UnitPrice, Quantity = i.Quantity,
            }).ToList(),
        }));
        return copy;
    }
}

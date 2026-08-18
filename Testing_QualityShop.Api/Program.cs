using System.Diagnostics;
using Testing_QualityShop.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Services.AddSingleton<InMemoryShopStore>();
builder.Services.AddSingleton<PriceCalculator>();
builder.Services.AddSingleton<ShopService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/products", (ShopService shop) => Results.Ok(shop.GetProducts()));

app.MapGet("/api/products/search", async (
    string? query,
    ShopService shop,
    IConfiguration configuration,
    ILogger<Program> logger) =>
{
    var stopwatch = Stopwatch.StartNew();
    if (configuration.GetValue("SIMULATE_SLOW_SEARCH", true))
    {
        await Task.Delay(100);
    }

    var products = shop.SearchProducts(query);
    stopwatch.Stop();
    logger.LogInformation(
        "Product search for {Query} returned {Count} products in {ElapsedMilliseconds} ms",
        query,
        products.Count,
        stopwatch.ElapsedMilliseconds);
    return Results.Ok(products);
});

app.MapGet("/api/products/{id:int}", (int id, ShopService shop) =>
    Results.Ok(shop.GetProduct(id)));

app.MapGet("/api/cart", (ShopService shop) => Results.Ok(CartResponse.From(shop)));

app.MapPost("/api/cart/items", (AddCartItemRequest request, ShopService shop, ILogger<Program> logger) =>
{
    if (request.Quantity == 0)
    {
        return Results.Ok(CartResponse.From(shop));
    }

    return ApiResults.Run(() =>
    {
        shop.AddToCart(request.ProductId, request.Quantity);
        logger.LogInformation(
            "Added product {ProductId} with quantity {Quantity} to cart",
            request.ProductId,
            request.Quantity);
        return Results.Ok(CartResponse.From(shop));
    }, logger);
});

app.MapPut("/api/cart/items/{productId:int}", (
    int productId,
    UpdateCartItemRequest request,
    ShopService shop,
    ILogger<Program> logger) =>
    ApiResults.Run(() =>
    {
        shop.UpdateCartItem(productId, request.Quantity);
        return Results.Ok(CartResponse.From(shop));
    }, logger));

app.MapDelete("/api/cart/items/{productId:int}", (
    int productId,
    ShopService shop,
    ILogger<Program> logger) =>
    ApiResults.Run(() =>
    {
        shop.RemoveCartItem(productId);
        return Results.Ok(CartResponse.From(shop));
    }, logger));

app.MapDelete("/api/cart", (ShopService shop) =>
{
    shop.ClearCart();
    return Results.Ok(CartResponse.From(shop));
});

app.MapPost("/api/cart/discount", (
    DiscountRequest request,
    ShopService shop,
    ILogger<Program> logger) =>
    ApiResults.Run(() =>
    {
        shop.ApplyDiscount(request.Code);
        return Results.Ok(CartResponse.From(shop));
    }, logger));

app.MapDelete("/api/cart/discount", (ShopService shop) =>
{
    shop.RemoveDiscount();
    return Results.Ok(CartResponse.From(shop));
});

app.MapPost("/api/orders", (ShopService shop, ILogger<Program> logger) =>
    ApiResults.Run(() =>
    {
        var order = shop.PlaceOrder();
        logger.LogInformation(
            "Created order {OrderId} with {LineCount} lines and total {Total}",
            order.Id,
            order.Lines.Count,
            order.Total);
        return Results.Created($"/api/orders/{order.Id}", order);
    }, logger));

app.MapGet("/api/orders", (ShopService shop) => Results.Ok(shop.GetOrders()));

app.MapGet("/api/orders/{id:int}", (int id, ShopService shop) =>
{
    var order = shop.GetOrder(id);
    return order is null
        ? Results.NotFound(new MessageResponse("Order not found."))
        : Results.Ok(order);
});

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapPost("/api/test/reset", (InMemoryShopStore store, ILogger<Program> logger) =>
    {
        store.Reset();
        logger.LogInformation("Test data was reset to seed values");
        return Results.Ok(new MessageResponse("Test data reset."));
    });
}

app.Run();

public partial class Program;

internal static class ApiResults
{
    public static IResult Run(Func<IResult> action, ILogger logger)
    {
        try
        {
            return action();
        }
        catch (ShopNotFoundException exception)
        {
            logger.LogWarning(exception, "Shop operation could not find a resource");
            return Results.NotFound(new MessageResponse(exception.Message));
        }
        catch (ShopConflictException exception)
        {
            logger.LogWarning(exception, "Shop operation resulted in a conflict");
            return Results.Conflict(new MessageResponse(exception.Message));
        }
        catch (ShopValidationException exception)
        {
            logger.LogWarning(exception, "Shop operation failed validation");
            return Results.BadRequest(new MessageResponse(exception.Message));
        }
    }
}

internal sealed record AddCartItemRequest(int ProductId, int Quantity);

internal sealed record UpdateCartItemRequest(int Quantity);

internal sealed record DiscountRequest(string? Code);

internal sealed record MessageResponse(string Message);

internal sealed record CartItemResponse(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

internal sealed record CartResponse(
    IReadOnlyList<CartItemResponse> Items,
    string? DiscountCode,
    decimal Subtotal,
    decimal Discount,
    decimal Shipping,
    decimal Total)
{
    public static CartResponse From(ShopService shop)
    {
        var cart = shop.GetCart();
        var totals = shop.GetCartTotals();
        return new CartResponse(
            cart.Items.Select(item => new CartItemResponse(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal)).ToList(),
            cart.DiscountCode,
            totals.Subtotal,
            totals.Discount,
            totals.Shipping,
            totals.Total);
    }
}

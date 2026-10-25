using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Testing_QualityShop.FunctionalCore.Test.Api;

[NonParallelizable]
public sealed class ContractTests
{
    private QualityShopApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Start()
    {
        _factory = new QualityShopApiFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public async Task Reset() => (await _client.PostAsync("/api/test/reset", null)).EnsureSuccessStatusCode();

    [OneTimeTearDown]
    public void Stop()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task Invalid_quantity_returns_bad_request_without_changing_cart(int quantity)
    {
        var response = await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await Read("/api/cart")).GetProperty("items").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task Unknown_product_returns_not_found_for_read_and_add()
    {
        Assert.That((await _client.GetAsync("/api/products/999")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var add = await _client.PostAsJsonAsync("/api/cart/items", new { productId = 999, quantity = 1 });
        Assert.That(add.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Repeated_add_returns_conflict_and_keeps_previous_quantity()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 3 })).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 3 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That((await Read("/api/cart")).GetProperty("items")[0].GetProperty("quantity").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task Discount_order_and_stock_can_be_read_back_through_http()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 2, quantity = 2 })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/cart/discount", new { code = "save10" })).EnsureSuccessStatusCode();

        var response = await _client.PostAsync("/api/orders", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var order = await Read(response.Headers.Location!.ToString());
        Assert.Multiple(() =>
        {
            Assert.That(order.GetProperty("subtotal").GetDecimal(), Is.EqualTo(798m));
            Assert.That(order.GetProperty("discount").GetDecimal(), Is.EqualTo(79.8m));
            Assert.That(order.GetProperty("shipping").GetDecimal(), Is.EqualTo(99m));
            Assert.That(order.GetProperty("total").GetDecimal(), Is.EqualTo(817.2m));
            Assert.That(order.GetProperty("lines")[0].GetProperty("quantity").GetInt32(), Is.EqualTo(2));
        });
        Assert.That((await Read("/api/products/2")).GetProperty("stockQuantity").GetInt32(), Is.EqualTo(10));
        Assert.That((await Read("/api/cart")).GetProperty("items").GetArrayLength(), Is.Zero);
        Assert.That((await Read("/api/orders")).GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public async Task Update_remove_discount_remove_item_and_clear_keep_contract()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 2, quantity = 1 })).EnsureSuccessStatusCode();
        (await _client.PutAsJsonAsync("/api/cart/items/2", new { quantity = 2 })).EnsureSuccessStatusCode();
        Assert.That((await Read("/api/cart")).GetProperty("subtotal").GetDecimal(), Is.EqualTo(798m));
        (await _client.PostAsJsonAsync("/api/cart/discount", new { code = "SAVE10" })).EnsureSuccessStatusCode();
        (await _client.DeleteAsync("/api/cart/discount")).EnsureSuccessStatusCode();
        Assert.That((await Read("/api/cart")).GetProperty("discount").GetDecimal(), Is.Zero);
        (await _client.DeleteAsync("/api/cart/items/2")).EnsureSuccessStatusCode();
        Assert.That((await Read("/api/cart")).GetProperty("items").GetArrayLength(), Is.Zero);
        (await _client.PostAsJsonAsync("/api/cart/discount", new { code = "SAVE10" })).EnsureSuccessStatusCode();
        (await _client.DeleteAsync("/api/cart")).EnsureSuccessStatusCode();
        Assert.That((await Read("/api/cart")).GetProperty("discountCode").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public async Task Empty_order_is_bad_request_and_search_is_case_insensitive()
    {
        Assert.That((await _client.PostAsync("/api/orders", null)).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var found = await Read("/api/products/search?query=MOUSE");
        Assert.That(found[0].GetProperty("id").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public async Task Reset_restores_stock_cart_discount_orders_and_identity()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 1 })).EnsureSuccessStatusCode();
        (await _client.PostAsync("/api/orders", null)).EnsureSuccessStatusCode();

        await Reset();

        Assert.That((await Read("/api/products/1")).GetProperty("stockQuantity").GetInt32(), Is.EqualTo(5));
        Assert.That((await Read("/api/orders")).GetArrayLength(), Is.Zero);
        var cart = await Read("/api/cart");
        Assert.That(cart.GetProperty("items").GetArrayLength(), Is.Zero);
        Assert.That(cart.GetProperty("discountCode").ValueKind, Is.EqualTo(JsonValueKind.Null));
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 1 })).EnsureSuccessStatusCode();
        var order = await _client.PostAsync("/api/orders", null);
        Assert.That(order.Headers.Location!.ToString(), Is.EqualTo("/api/orders/1"));
    }

    private async Task<JsonElement> Read(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

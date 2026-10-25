using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Testing_QualityShop.SqlServer.Test.Api;

[NonParallelizable]
[Category("SqlIntegration")]
public sealed class ShopApiTests
{
    private QualityShopApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new QualityShopApiFactory();
        _client = _factory.CreateClient();
    }

    [SetUp]
    public async Task SetUp()
    {
        var response = await _client.PostAsync("/api/test/reset", content: null);
        response.EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        _factory.DropDatabase();
    }

    [Test]
    public async Task Get_products_returns_seeded_products()
    {
        var response = await _client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetArrayLength(), Is.EqualTo(6));
    }

    [Test]
    public async Task Post_cart_item_with_valid_input_returns_updated_cart()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/cart/items",
            new { productId = 1, quantity = 2 });

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("items")[0];
        Assert.Multiple(() =>
        {
            Assert.That(item.GetProperty("productName").GetString(), Is.EqualTo("Mechanical Keyboard"));
            Assert.That(item.GetProperty("quantity").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Post_order_with_valid_cart_returns_created_order()
    {
        var addResponse = await _client.PostAsJsonAsync(
            "/api/cart/items",
            new { productId = 4, quantity = 1 });
        addResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsync("/api/orders", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task Get_unknown_order_returns_not_found()
    {
        var response = await _client.GetAsync("/api/orders/999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}

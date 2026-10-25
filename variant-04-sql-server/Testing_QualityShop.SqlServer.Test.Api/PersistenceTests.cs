using System.Net.Http.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Testing_QualityShop.SqlServer.Core;
using Testing_QualityShop.SqlServer.Infrastructure;

namespace Testing_QualityShop.SqlServer.Test.Api;

[NonParallelizable]
[Category("SqlIntegration")]
public sealed class PersistenceTests
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
        _factory.DropDatabase();
    }

    [Test]
    public async Task Products_come_from_database_instead_of_hardcoded_api_data()
    {
        using var connection = new SqlConnection(_factory.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Products SET Name=@Name WHERE Id=1", new { Name = "Database probe" });

        var product = await _client.GetFromJsonAsync<Product>("/api/products/1");

        Assert.That(product!.Name, Is.EqualTo("Database probe"));
    }

    [TestCase("/api/products")]
    [TestCase("/api/products/1")]
    [TestCase("/api/products/search?query=MOUSE")]
    [TestCase("/api/orders")]
    [TestCase("/api/orders/1")]
    public async Task Catalog_and_order_reads_do_not_wait_for_the_mutation_metadata_lock(string path)
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 2, quantity = 1 })).EnsureSuccessStatusCode();
        (await _client.PostAsync("/api/orders", null)).EnsureSuccessStatusCode();
        var repository = new SqlShopRepository(_factory.ConnectionString);
        using var heldMutation = repository.OpenSession();

        // The generous timeout guards against a blocked request, not a latency budget.
        // The old read path tried to acquire this same exclusive metadata lock.
        using var response = await Task.Run(() => _client.GetAsync(path)).WaitAsync(TimeSpan.FromSeconds(10));

        response.EnsureSuccessStatusCode();
    }

    [TestCase("%", 1)]
    [TestCase("_", 1)]
    [TestCase("[", 1)]
    [TestCase("' OR 1=1--", 0)]
    public async Task Sql_search_uses_literal_parameterized_text(string query, int expectedCount)
    {
        using var connection = new SqlConnection(_factory.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Products SET Name=@Name WHERE Id=1", new { Name = "Keyboard 100%_[" });

        var products = await _client.GetFromJsonAsync<Product[]>("/api/products/search?query=" + Uri.EscapeDataString(query));

        Assert.That(products, Has.Length.EqualTo(expectedCount));
    }

    [Test]
    public async Task New_application_instance_reads_persisted_cart_then_order_lines_and_money()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 2, quantity = 2 })).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/cart/discount", new { code = "save10" })).EnsureSuccessStatusCode();
        using var secondHost = new QualityShopApiFactory(_factory.ConnectionString);
        using var secondClient = secondHost.CreateClient();
        var cart = await secondClient.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/cart");
        Assert.That(cart.GetProperty("items")[0].GetProperty("quantity").GetInt32(), Is.EqualTo(2));

        var response = await secondClient.PostAsync("/api/orders", null);
        response.EnsureSuccessStatusCode();
        var order = await _client.GetFromJsonAsync<Order>(response.Headers.Location!);

        using var connection = new SqlConnection(_factory.ConnectionString);
        Assert.Multiple(() =>
        {
            Assert.That(order!.Total, Is.EqualTo(817.2m));
            Assert.That(order.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(order.Lines.Single().Quantity, Is.EqualTo(2));
            Assert.That(connection.QuerySingle<decimal>("SELECT Total FROM dbo.Orders WHERE Id=1"), Is.EqualTo(817.2m));
            Assert.That(connection.QuerySingle<int>("SELECT Quantity FROM dbo.OrderLines WHERE OrderId=1"), Is.EqualTo(2));
            Assert.That(connection.QuerySingle<int>("SELECT StockQuantity FROM dbo.Products WHERE Id=2"), Is.EqualTo(10));
            Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM dbo.CartItems"), Is.Zero);
        });
    }

    [Test]
    public async Task Stock_changed_in_database_prevents_order_and_preserves_cart()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 1, quantity = 3 })).EnsureSuccessStatusCode();
        using var connection = new SqlConnection(_factory.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Products SET StockQuantity=2 WHERE Id=1");

        var response = await _client.PostAsync("/api/orders", null);

        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Conflict));
        Assert.Multiple(() =>
        {
            Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM dbo.Orders"), Is.Zero);
            Assert.That(connection.QuerySingle<int>("SELECT Quantity FROM dbo.CartItems WHERE ProductId=1"), Is.EqualTo(3));
            Assert.That(connection.QuerySingle<int>("SELECT StockQuantity FROM dbo.Products WHERE Id=1"), Is.EqualTo(2));
        });
    }

    [Test]
    public void Sql_constraint_failure_rolls_back_previous_writes_in_the_same_transaction()
    {
        var repository = new SqlShopRepository(_factory.ConnectionString);
        using (var session = repository.OpenSession())
        {
            session.State.Products.Single(p => p.Id == 1).StockQuantity = 0;
            // Deliberately bypass the service to exercise a real database constraint.
            session.State.Cart.Items.Add(new CartItem
            {
                ProductId = 1, ProductName = "Invalid persistence input", UnitPrice = 899m, Quantity = 0,
            });
            Assert.Throws<SqlException>(() => session.Commit());
        }

        using var read = repository.OpenSession();
        Assert.Multiple(() =>
        {
            Assert.That(read.State.Products.Single(p => p.Id == 1).StockQuantity, Is.EqualTo(5));
            Assert.That(read.State.Cart.Items, Is.Empty);
        });
    }

    [Test]
    public async Task Separate_repository_instances_cannot_overwrite_each_others_cart_changes()
    {
        var first = new ShopService(new SqlShopRepository(_factory.ConnectionString), new PriceCalculator(), TimeProvider.System);
        var second = new ShopService(new SqlShopRepository(_factory.ConnectionString), new PriceCalculator(), TimeProvider.System);

        await Task.WhenAll(Task.Run(() => first.AddToCart(2, 2)), Task.Run(() => second.AddToCart(2, 3)));

        Assert.That(first.GetCart().Items.Single().Quantity, Is.EqualTo(5));
    }

    [Test]
    public async Task Reinitialization_does_not_reseed_existing_data_but_reset_does()
    {
        (await _client.PostAsJsonAsync("/api/cart/items", new { productId = 3, quantity = 1 })).EnsureSuccessStatusCode();
        new DatabaseInitializer(_factory.ConnectionString).Initialize();
        var repository = new SqlShopRepository(_factory.ConnectionString);
        using (var read = repository.OpenSession())
        {
            Assert.That(read.State.Cart.Items.Single().ProductId, Is.EqualTo(3));
        }

        repository.Reset();
        repository.Reset();

        using var clean = repository.OpenSession();
        Assert.Multiple(() =>
        {
            Assert.That(clean.State.Products, Has.Count.EqualTo(6));
            Assert.That(clean.State.Products.Sum(p => p.StockQuantity), Is.EqualTo(42));
            Assert.That(clean.State.Cart.Items, Is.Empty);
            Assert.That(clean.State.Orders, Is.Empty);
            Assert.That(clean.State.NextOrderId, Is.EqualTo(1));
        });
    }
}

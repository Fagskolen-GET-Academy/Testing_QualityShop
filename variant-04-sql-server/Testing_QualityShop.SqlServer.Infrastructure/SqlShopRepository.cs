using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Testing_QualityShop.SqlServer.Core;

namespace Testing_QualityShop.SqlServer.Infrastructure;

public sealed class SqlShopRepository(string connectionString) : IShopRepository
{
    // Catalog reads do not touch cart, metadata or order history, or open a write session.
    public IReadOnlyList<Product> GetProducts()
    {
        using var connection = new SqlConnection(connectionString);
        return connection.Query<Product>(
            "SELECT Id, Name, Price, StockQuantity FROM dbo.Products ORDER BY Id").ToList();
    }

    public Product? GetProduct(int id)
    {
        using var connection = new SqlConnection(connectionString);
        return connection.QuerySingleOrDefault<Product>(
            "SELECT Id, Name, Price, StockQuantity FROM dbo.Products WHERE Id=@Id", new { Id = id });
    }

    public IReadOnlyList<Product> SearchProducts(string? query)
    {
        using var connection = new SqlConnection(connectionString);
        // CHARINDEX treats %, _ and [ as ordinary search text, not LIKE wildcards.
        return connection.Query<Product>(
            """
            SELECT Id, Name, Price, StockQuantity FROM dbo.Products
            WHERE @Query IS NULL OR CHARINDEX(@Query, Name COLLATE Latin1_General_100_CI_AS) > 0
            ORDER BY Id
            """, new { Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim() }).ToList();
    }

    public Cart GetCart()
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        // A short shared read transaction keeps discount and lines consistent.
        // Mutations acquire an exclusive metadata lock before changing either table.
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        var cart = new Cart
        {
            DiscountCode = connection.QuerySingleOrDefault<string>(
                "SELECT DiscountCode FROM dbo.ShopMetadata WHERE Id=1", transaction: transaction),
        };
        cart.Items.AddRange(connection.Query<CartItem>(
            "SELECT ProductId, ProductName, UnitPrice, Quantity FROM dbo.CartItems ORDER BY ProductId", transaction: transaction));
        transaction.Commit();
        return cart;
    }

    public IReadOnlyList<Order> GetOrders()
    {
        using var connection = new SqlConnection(connectionString);
        return ReadOrders(connection, id: null);
    }

    public Order? GetOrder(int id)
    {
        using var connection = new SqlConnection(connectionString);
        return ReadOrders(connection, id).SingleOrDefault();
    }

    private static List<Order> ReadOrders(SqlConnection connection, int? id)
    {
        var orders = new Dictionary<int, Order>();
        // One SELECT joins headers and lines. GetOrder filters in SQL, not in memory.
        _ = connection.Query<Order, OrderLine, Order>(
            """
            SELECT o.Id, o.CreatedAt, o.Subtotal, o.Discount, o.Shipping, o.Total,
                   l.ProductId, l.ProductName, l.UnitPrice, l.Quantity
            FROM dbo.Orders o
            INNER JOIN dbo.OrderLines l ON l.OrderId=o.Id
            WHERE @Id IS NULL OR o.Id=@Id
            ORDER BY o.Id, l.ProductId
            """,
            (header, line) =>
            {
                if (!orders.TryGetValue(header.Id, out var order))
                {
                    order = new Order
                    {
                        Id = header.Id,
                        CreatedAt = DateTime.SpecifyKind(header.CreatedAt, DateTimeKind.Utc),
                        Subtotal = header.Subtotal, Discount = header.Discount,
                        Shipping = header.Shipping, Total = header.Total, Lines = [],
                    };
                    orders.Add(order.Id, order);
                }

                order.Lines.Add(line);
                return order;
            }, new { Id = id }, splitOn: "ProductId");
        return orders.Values.ToList();
    }

    public IShopSession OpenSession() => new Session(connectionString);

    public void Reset()
    {
        using var session = new Session(connectionString);
        session.Reset();
    }

    private sealed class Session : IShopSession
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;
        private readonly HashSet<int> _existingOrderIds;

        public Session(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            try
            {
                _connection.Open();
                _transaction = _connection.BeginTransaction(IsolationLevel.Serializable);
                // One shared shop/cart: serialize mutations across processes, not just threads.
                var nextId = _connection.QuerySingle<int>(
                    "SELECT NextOrderId FROM dbo.ShopMetadata WITH (XLOCK, HOLDLOCK) WHERE Id=1", transaction: _transaction);
                State = new ShopState { NextOrderId = nextId };
                State.Cart.DiscountCode = _connection.QuerySingleOrDefault<string>(
                    "SELECT DiscountCode FROM dbo.ShopMetadata WHERE Id=1", transaction: _transaction);
                State.Products.AddRange(_connection.Query<Product>(
                    "SELECT Id, Name, Price, StockQuantity FROM dbo.Products ORDER BY Id", transaction: _transaction));
                State.Cart.Items.AddRange(_connection.Query<CartItem>(
                    "SELECT ProductId, ProductName, UnitPrice, Quantity FROM dbo.CartItems ORDER BY ProductId", transaction: _transaction));
                var orders = _connection.Query<Order>(
                    "SELECT Id, CreatedAt, Subtotal, Discount, Shipping, Total FROM dbo.Orders ORDER BY Id", transaction: _transaction);
                foreach (var order in orders)
                {
                    State.Orders.Add(new Order
                    {
                        Id = order.Id,
                        CreatedAt = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc),
                        Subtotal = order.Subtotal, Discount = order.Discount, Shipping = order.Shipping, Total = order.Total,
                        Lines = _connection.Query<OrderLine>(
                            "SELECT ProductId, ProductName, UnitPrice, Quantity FROM dbo.OrderLines WHERE OrderId=@Id ORDER BY ProductId",
                            new { order.Id }, _transaction).ToList(),
                    });
                }

                _existingOrderIds = State.Orders.Select(o => o.Id).ToHashSet();
            }
            catch
            {
                _connection.Dispose();
                throw;
            }
        }

        public ShopState State { get; }

        public void Commit()
        {
            _connection.Execute("UPDATE dbo.Products SET StockQuantity=@StockQuantity WHERE Id=@Id", State.Products, _transaction);
            _connection.Execute("DELETE FROM dbo.CartItems", transaction: _transaction);
            _connection.Execute(
                "INSERT dbo.CartItems (ProductId, ProductName, UnitPrice, Quantity) VALUES (@ProductId, @ProductName, @UnitPrice, @Quantity)",
                State.Cart.Items, _transaction);
            foreach (var order in State.Orders.Where(o => !_existingOrderIds.Contains(o.Id)))
            {
                _connection.Execute(
                    "INSERT dbo.Orders (Id, CreatedAt, Subtotal, Discount, Shipping, Total) VALUES (@Id, @CreatedAt, @Subtotal, @Discount, @Shipping, @Total)",
                    order, _transaction);
                foreach (var line in order.Lines)
                {
                    _connection.Execute(
                        "INSERT dbo.OrderLines (OrderId, ProductId, ProductName, UnitPrice, Quantity) VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity)",
                        new { OrderId = order.Id, line.ProductId, line.ProductName, line.UnitPrice, line.Quantity }, _transaction);
                }
            }

            _connection.Execute("UPDATE dbo.ShopMetadata SET NextOrderId=@NextOrderId, DiscountCode=@DiscountCode WHERE Id=1",
                new { State.NextOrderId, State.Cart.DiscountCode }, _transaction);
            _transaction.Commit();
        }

        public void Reset()
        {
            _connection.Execute(DatabaseInitializer.ReadScript("Seed.sql"), transaction: _transaction);
            _transaction.Commit();
        }

        public void Dispose()
        {
            // An uncommitted transaction rolls back, including a partially failed SQL write.
            _transaction.Dispose();
            _connection.Dispose();
        }
    }
}

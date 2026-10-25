DELETE FROM dbo.OrderLines;
DELETE FROM dbo.Orders;
DELETE FROM dbo.CartItems;
DELETE FROM dbo.Products;
DELETE FROM dbo.ShopMetadata;
INSERT dbo.ShopMetadata (Id, NextOrderId, DiscountCode) VALUES (1, 1, NULL);
INSERT dbo.Products (Id, Name, Price, StockQuantity) VALUES
    (1, N'Mechanical Keyboard', 899.00, 5),
    (2, N'Wireless Mouse', 399.00, 12),
    (3, N'USB-C Hub', 499.00, 8),
    (4, N'Webcam', 749.00, 4),
    (5, N'Laptop Stand', 599.00, 10),
    (6, N'Headset', 999.00, 3);

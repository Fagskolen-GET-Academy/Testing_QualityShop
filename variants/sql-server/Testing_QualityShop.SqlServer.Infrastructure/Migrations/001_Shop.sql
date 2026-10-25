CREATE TABLE dbo.Products (
    Id int NOT NULL PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Price decimal(18,2) NOT NULL CHECK (Price >= 0),
    StockQuantity int NOT NULL CHECK (StockQuantity >= 0)
);
CREATE TABLE dbo.ShopMetadata (
    Id int NOT NULL PRIMARY KEY CHECK (Id = 1),
    NextOrderId int NOT NULL,
    DiscountCode nvarchar(50) NULL
);
CREATE TABLE dbo.CartItems (
    ProductId int NOT NULL PRIMARY KEY REFERENCES dbo.Products(Id),
    ProductName nvarchar(200) NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    Quantity int NOT NULL CHECK (Quantity >= 1)
);
CREATE TABLE dbo.Orders (
    Id int NOT NULL PRIMARY KEY,
    CreatedAt datetime2(7) NOT NULL,
    Subtotal decimal(18,2) NOT NULL,
    Discount decimal(18,2) NOT NULL,
    Shipping decimal(18,2) NOT NULL,
    Total decimal(18,2) NOT NULL
);
CREATE TABLE dbo.OrderLines (
    OrderId int NOT NULL REFERENCES dbo.Orders(Id),
    ProductId int NOT NULL REFERENCES dbo.Products(Id),
    ProductName nvarchar(200) NOT NULL,
    UnitPrice decimal(18,2) NOT NULL,
    Quantity int NOT NULL CHECK (Quantity >= 1),
    PRIMARY KEY (OrderId, ProductId)
);

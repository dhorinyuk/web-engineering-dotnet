-- Ordering bounded context database schema (Microsoft SQL Server)

IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistory;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

-- Customer identity snapshot in ordering context
CREATE TABLE dbo.Customers (
    CustomerId  INT IDENTITY(1,1) NOT NULL,
    ExternalId  UNIQUEIDENTIFIER NOT NULL,
    FullName    NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(255) NOT NULL,
    Phone       NVARCHAR(30) NOT NULL,
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_Customers_CreatedBy DEFAULT (SUSER_SNAME()),
    UpdatedAt   DATETIME2(7) NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT (0),

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customers_ExternalId UNIQUE NONCLUSTERED (ExternalId),
    CONSTRAINT UQ_Customers_Email UNIQUE NONCLUSTERED (Email),
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE '%_@_%._%')
);
GO

-- Core order aggregate root with optimistic concurrency token
CREATE TABLE dbo.Orders (
    OrderId     INT IDENTITY(1,1) NOT NULL,
    OrderNumber NVARCHAR(32) NOT NULL,
    CustomerId  INT NOT NULL,
    Status      NVARCHAR(32) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT ('Submitted'),
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT (0.00),
    RowVersion  ROWVERSION NOT NULL,
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_Orders_CreatedBy DEFAULT (SUSER_SNAME()),
    UpdatedAt   DATETIME2(7) NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL CONSTRAINT DF_Orders_IsDeleted DEFAULT (0),

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE NONCLUSTERED (OrderNumber),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('Submitted', 'AwaitingValidation', 'StockConfirmed', 'Paid', 'Shipped', 'Cancelled')),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0.00)
);
GO

-- 1:1 order delivery details where PK is FK
CREATE TABLE dbo.OrderDetails (
    OrderId         INT NOT NULL,
    RecipientName   NVARCHAR(100) NOT NULL,
    RecipientPhone  NVARCHAR(30) NOT NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    City            NVARCHAR(100) NOT NULL,
    PostalCode      NVARCHAR(20) NULL,
    ShippingMethod  NVARCHAR(50) NOT NULL,
    TrackingNumber  NVARCHAR(100) NULL,
    DeliveredAt     DATETIME2(7) NULL,
    CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_OrderDetails_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy       NVARCHAR(100) NOT NULL CONSTRAINT DF_OrderDetails_CreatedBy DEFAULT (SUSER_SNAME()),
    UpdatedAt       DATETIME2(7) NULL,
    UpdatedBy       NVARCHAR(100) NULL,
    IsDeleted       BIT NOT NULL CONSTRAINT DF_OrderDetails_IsDeleted DEFAULT (0),

    CONSTRAINT PK_OrderDetails PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE
);
GO

-- Local replicated product cache without external database foreign keys
CREATE TABLE dbo.Products (
    ProductId   INT NOT NULL,
    Sku         NVARCHAR(64) NOT NULL,
    Name        NVARCHAR(200) NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    IsAvailable BIT NOT NULL CONSTRAINT DF_Products_IsAvailable DEFAULT (1),
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_Products_CreatedBy DEFAULT (SUSER_SNAME()),
    UpdatedAt   DATETIME2(7) NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT (0),

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT UQ_Products_Sku UNIQUE NONCLUSTERED (Sku),
    CONSTRAINT CK_Products_Price CHECK (Price > 0.00)
);
GO

-- M:N join entity with composite primary key and immutable item snapshots
CREATE TABLE dbo.OrderItems (
    OrderId     INT NOT NULL,
    ProductId   INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL,
    Units       INT NOT NULL,
    SubTotal    AS (UnitPrice * Units) PERSISTED,
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_OrderItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_OrderItems_CreatedBy DEFAULT (SUSER_SNAME()),
    UpdatedAt   DATETIME2(7) NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL CONSTRAINT DF_OrderItems_IsDeleted DEFAULT (0),

    CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (OrderId, ProductId),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0.00),
    CONSTRAINT CK_OrderItems_Units CHECK (Units > 0)
);
GO

-- Append-only audit history of order transitions
CREATE TABLE dbo.OrderStatusHistory (
    HistoryId   INT IDENTITY(1,1) NOT NULL,
    OrderId     INT NOT NULL,
    FromStatus  NVARCHAR(32) NULL,
    ToStatus    NVARCHAR(32) NOT NULL,
    Reason      NVARCHAR(255) NULL,
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_OrderStatusHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CreatedBy   NVARCHAR(100) NOT NULL CONSTRAINT DF_OrderStatusHistory_CreatedBy DEFAULT (SUSER_SNAME()),

    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY CLUSTERED (HistoryId),
    CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE
);
GO

-- Filtered index for active order lookups
CREATE NONCLUSTERED INDEX IX_Orders_Active
ON dbo.Orders (OrderId, CustomerId, Status)
WHERE IsDeleted = 0;
GO

-- Composite index for customer order history queries
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_Status
ON dbo.Orders (CustomerId, Status);
GO

-- Covering index on product order items
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId
ON dbo.OrderItems (ProductId)
INCLUDE (Units, UnitPrice);
GO

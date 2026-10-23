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
    Id          BIGINT IDENTITY(1,1) NOT NULL,
    ExternalId  UNIQUEIDENTIFIER NOT NULL,
    FullName    NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(255) NOT NULL,
    Phone       NVARCHAR(30) NOT NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Customers_ExternalId UNIQUE NONCLUSTERED (ExternalId),
    CONSTRAINT UQ_Customers_Email UNIQUE NONCLUSTERED (Email),
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE '%_@_%._%')
);
GO

-- Core order aggregate root with optimistic concurrency token
CREATE TABLE dbo.Orders (
    Id          BIGINT IDENTITY(1,1) NOT NULL,
    OrderNumber NVARCHAR(32) NOT NULL,
    CustomerId  BIGINT NOT NULL,
    Status      NVARCHAR(32) NOT NULL DEFAULT ('Submitted'),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT (0.00),
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    RowVersion  ROWVERSION,

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE NONCLUSTERED (OrderNumber),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('Submitted', 'AwaitingValidation', 'StockConfirmed', 'Paid', 'Shipped', 'Cancelled')),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0.00)
);
GO

-- 1:1 order delivery details where PK is FK
CREATE TABLE dbo.OrderDetails (
    OrderId         BIGINT PRIMARY KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    RecipientName   NVARCHAR(100) NOT NULL,
    RecipientPhone  NVARCHAR(30) NOT NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    City            NVARCHAR(100) NOT NULL,
    PostalCode      NVARCHAR(20) NULL,
    ShippingMethod  NVARCHAR(50) NOT NULL,
    TrackingNumber  NVARCHAR(100) NULL,
    DeliveredAt     DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
    UpdatedAt       DATETIME2 NULL,
    UpdatedBy       NVARCHAR(100) NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0
);
GO

-- Local replicated product cache without external database foreign keys
CREATE TABLE dbo.Products (
    Id          BIGINT NOT NULL,
    Sku         NVARCHAR(64) NOT NULL,
    Name        NVARCHAR(200) NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT (1),
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Products_Sku UNIQUE NONCLUSTERED (Sku),
    CONSTRAINT CK_Products_Price CHECK (Price > 0.00)
);
GO

-- M:N join entity with composite primary key and immutable item snapshots
CREATE TABLE dbo.OrderItems (
    OrderId     BIGINT NOT NULL REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductId   BIGINT NOT NULL REFERENCES dbo.Products(Id),
    ProductName NVARCHAR(200) NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL,
    Units       INT NOT NULL,
    SubTotal    AS (UnitPrice * Units) PERSISTED,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(100) NOT NULL DEFAULT SUSER_SNAME(),
    UpdatedAt   DATETIME2 NULL,
    UpdatedBy   NVARCHAR(100) NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,

    PRIMARY KEY (OrderId, ProductId),
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0.00),
    CONSTRAINT CK_OrderItems_Units CHECK (Units > 0)
);
GO

-- Append-only audit history of order transitions
CREATE TABLE dbo.OrderStatusHistory (
    HistoryId   BIGINT IDENTITY(1,1) NOT NULL,
    OrderId     BIGINT NOT NULL REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    OldStatus   NVARCHAR(30) NULL,
    NewStatus   NVARCHAR(30) NOT NULL,
    ChangedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Comment     NVARCHAR(500) NULL,

    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY CLUSTERED (HistoryId)
);
GO

-- Index for customer order history queries
CREATE INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status);
GO

-- Filtered index for active non-deleted orders
CREATE INDEX IX_Orders_Active ON dbo.Orders (CustomerId) WHERE IsDeleted = 0;
GO

-- Covering index for product orders
CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId) INCLUDE (Units, UnitPrice);
GO

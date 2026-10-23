-- ============================================================================
-- Проєкт №1: Контекст замовлень та виконання (Orders & Fulfillment Context)
-- СУБД: Microsoft SQL Server
-- Архітектура: Bounded Context (DDD) / Ізольована схема
-- Правило автономності: Жодних FK на таблиці інших сервісів (каталог, юзери тощо)
-- ============================================================================

-- Безпечне очищення існуючих об'єктів для повторного накатування схеми
IF OBJECT_ID(N'dbo.OrderPromotions', N'U') IS NOT NULL DROP TABLE dbo.OrderPromotions;
IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistory;
IF OBJECT_ID(N'dbo.PaymentTransactions', N'U') IS NOT NULL DROP TABLE dbo.PaymentTransactions;
IF OBJECT_ID(N'dbo.OrderDeliveries', N'U') IS NOT NULL DROP TABLE dbo.OrderDeliveries;
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Promotions', N'U') IS NOT NULL DROP TABLE dbo.Promotions;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

-- ============================================================================
-- 1. Таблиця клієнтів (Customers)
-- Зберігає локальний профіль замовника в контексті продажів.
-- ExternalUserId - UUID із сервісу ідентифікації (без FK на іншу БД).
-- ============================================================================
CREATE TABLE dbo.Customers (
    CustomerId      INT IDENTITY(1,1) NOT NULL,
    ExternalUserId  UNIQUEIDENTIFIER NOT NULL,
    FirstName       NVARCHAR(50) NOT NULL,
    LastName        NVARCHAR(50) NOT NULL,
    Email           NVARCHAR(100) NOT NULL,
    Phone           NVARCHAR(20) NOT NULL,
    CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(7) NULL,
    IsDeleted       BIT NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (CustomerId),
    CONSTRAINT UQ_Customers_ExternalUserId UNIQUE NONCLUSTERED (ExternalUserId),
    CONSTRAINT CK_Customers_Email CHECK (Email LIKE '%_@_%._%'),
    CONSTRAINT CK_Customers_Phone CHECK (LEN(Phone) >= 7)
);
GO

-- ============================================================================
-- 2. Таблиця замовлень (Orders)
-- Головна сутність агрегату замовлення.
-- Підтримує оптимістичне блокування через RowVersion та м'яке видалення IsDeleted.
-- ============================================================================
CREATE TABLE dbo.Orders (
    OrderId         INT IDENTITY(1,1) NOT NULL,
    OrderNumber     NVARCHAR(30) NOT NULL,
    CustomerId      INT NOT NULL,
    OrderStatus     NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_OrderStatus DEFAULT ('Draft'),
    SubTotal        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_SubTotal DEFAULT (0.00),
    ShippingFee     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_ShippingFee DEFAULT (0.00),
    DiscountAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_DiscountAmount DEFAULT (0.00),
    TotalAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT (0.00),
    OrderDate       DATETIME2(7) NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT (SYSUTCDATETIME()),
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(7) NULL,
    IsDeleted       BIT NOT NULL CONSTRAINT DF_Orders_IsDeleted DEFAULT (0),
    RowVersion      ROWVERSION NOT NULL,

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT UQ_Orders_OrderNumber UNIQUE NONCLUSTERED (OrderNumber),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT CK_Orders_OrderStatus CHECK (OrderStatus IN ('Draft', 'Pending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled')),
    CONSTRAINT CK_Orders_SubTotal CHECK (SubTotal >= 0),
    CONSTRAINT CK_Orders_ShippingFee CHECK (ShippingFee >= 0),
    CONSTRAINT CK_Orders_DiscountAmount CHECK (DiscountAmount >= 0),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);
GO

-- ============================================================================
-- 3. Таблиця позицій замовлення (OrderItems) - Зв'язок 1:N з Orders
-- Дотримується патерну Price Snapshotting: фіксує SKU, Name та UnitPrice
-- на момент покупки, без зовнішнього ключа на таблицю товарів каталогу.
-- ============================================================================
CREATE TABLE dbo.OrderItems (
    OrderItemId         INT IDENTITY(1,1) NOT NULL,
    OrderId             INT NOT NULL,
    ExternalProductId   INT NOT NULL, -- ID товару з Проєкту №2 (без FK!)
    ProductSKU          NVARCHAR(50) NOT NULL,
    ProductName         NVARCHAR(200) NOT NULL,
    UnitPrice           DECIMAL(18,2) NOT NULL,
    Quantity            INT NOT NULL,
    TotalPrice          AS (UnitPrice * Quantity) PERSISTED,
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_OrderItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(7) NULL,
    IsDeleted           BIT NOT NULL CONSTRAINT DF_OrderItems_IsDeleted DEFAULT (0),

    CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (OrderItemId),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0)
);
GO

-- ============================================================================
-- 4. Таблиця доставки замовлення (OrderDeliveries) - Зв'язок 1:1 з Orders
-- Унікальний індекс на OrderId гарантує відношення строго 1:1.
-- Фіксує логістичний знімок адреси та статус служби перевезення.
-- ============================================================================
CREATE TABLE dbo.OrderDeliveries (
    DeliveryId          INT IDENTITY(1,1) NOT NULL,
    OrderId             INT NOT NULL,
    Carrier             NVARCHAR(50) NOT NULL,
    TrackingNumber      NVARCHAR(100) NULL,
    RecipientFullName   NVARCHAR(100) NOT NULL,
    RecipientPhone      NVARCHAR(20) NOT NULL,
    City                NVARCHAR(100) NOT NULL,
    PostalCode          NVARCHAR(20) NULL,
    DeliveryAddress     NVARCHAR(250) NOT NULL,
    DeliveryStatus      NVARCHAR(30) NOT NULL CONSTRAINT DF_OrderDeliveries_Status DEFAULT ('AwaitingDispatch'),
    DispatchedAt        DATETIME2(7) NULL,
    DeliveredAt         DATETIME2(7) NULL,
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_OrderDeliveries_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(7) NULL,
    IsDeleted           BIT NOT NULL CONSTRAINT DF_OrderDeliveries_IsDeleted DEFAULT (0),
    RowVersion          ROWVERSION NOT NULL,

    CONSTRAINT PK_OrderDeliveries PRIMARY KEY CLUSTERED (DeliveryId),
    CONSTRAINT UQ_OrderDeliveries_OrderId UNIQUE NONCLUSTERED (OrderId),
    CONSTRAINT FK_OrderDeliveries_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT CK_OrderDeliveries_Status CHECK (DeliveryStatus IN ('AwaitingDispatch', 'Dispatched', 'InTransit', 'Delivered', 'Returned'))
);
GO

-- ============================================================================
-- 5. Таблиця платіжних транзакцій (PaymentTransactions) - Зв'язок 1:N з Orders
-- Дозволяє часткові, повторні оплати та фіксує статус платіжного шлюзу.
-- ============================================================================
CREATE TABLE dbo.PaymentTransactions (
    PaymentId               INT IDENTITY(1,1) NOT NULL,
    OrderId                 INT NOT NULL,
    TransactionReference    NVARCHAR(100) NOT NULL,
    PaymentMethod           NVARCHAR(50) NOT NULL,
    Amount                  DECIMAL(18,2) NOT NULL,
    Currency                NVARCHAR(3) NOT NULL CONSTRAINT DF_PaymentTransactions_Currency DEFAULT ('UAH'),
    Status                  NVARCHAR(30) NOT NULL CONSTRAINT DF_PaymentTransactions_Status DEFAULT ('Pending'),
    PaymentDate             DATETIME2(7) NOT NULL CONSTRAINT DF_PaymentTransactions_PaymentDate DEFAULT (SYSUTCDATETIME()),
    ErrorMessage            NVARCHAR(500) NULL,
    CreatedAt               DATETIME2(7) NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(7) NULL,
    IsDeleted               BIT NOT NULL CONSTRAINT DF_PaymentTransactions_IsDeleted DEFAULT (0),

    CONSTRAINT PK_PaymentTransactions PRIMARY KEY CLUSTERED (PaymentId),
    CONSTRAINT UQ_PaymentTransactions_Reference UNIQUE NONCLUSTERED (TransactionReference),
    CONSTRAINT FK_PaymentTransactions_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId),
    CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount > 0),
    CONSTRAINT CK_PaymentTransactions_Currency CHECK (Currency IN ('UAH', 'USD', 'EUR')),
    CONSTRAINT CK_PaymentTransactions_Status CHECK (Status IN ('Pending', 'Success', 'Failed', 'Refunded')),
    CONSTRAINT CK_PaymentTransactions_Method CHECK (PaymentMethod IN ('CreditCard', 'LiqPay', 'ApplePay', 'GooglePay', 'BankTransfer', 'CashOnDelivery'))
);
GO

-- ============================================================================
-- 6. Таблиця аудиту статусів (OrderStatusHistory) - Зв'язок 1:N з Orders
-- Незмінний журнал (Append-Only Audit Log) переходів станів замовлення.
-- ============================================================================
CREATE TABLE dbo.OrderStatusHistory (
    HistoryId       INT IDENTITY(1,1) NOT NULL,
    OrderId         INT NOT NULL,
    OldStatus       NVARCHAR(30) NULL,
    NewStatus       NVARCHAR(30) NOT NULL,
    ChangedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_OrderStatusHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
    ChangedBy       NVARCHAR(100) NOT NULL CONSTRAINT DF_OrderStatusHistory_ChangedBy DEFAULT ('SYSTEM'),
    Reason          NVARCHAR(255) NULL,

    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY CLUSTERED (HistoryId),
    CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 7. Таблиця промо-кампаній (Promotions)
-- Маркетингові акції та знижки на автотовари/інструменти.
-- ============================================================================
CREATE TABLE dbo.Promotions (
    PromotionId         INT IDENTITY(1,1) NOT NULL,
    PromoCode           NVARCHAR(50) NOT NULL,
    DiscountPercent     DECIMAL(5,2) NOT NULL,
    MaxDiscountAmount   DECIMAL(18,2) NULL,
    StartDate           DATETIME2(7) NOT NULL,
    EndDate             DATETIME2(7) NOT NULL,
    IsActive            BIT NOT NULL CONSTRAINT DF_Promotions_IsActive DEFAULT (1),
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_Promotions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(7) NULL,
    IsDeleted           BIT NOT NULL CONSTRAINT DF_Promotions_IsDeleted DEFAULT (0),

    CONSTRAINT PK_Promotions PRIMARY KEY CLUSTERED (PromotionId),
    CONSTRAINT UQ_Promotions_PromoCode UNIQUE NONCLUSTERED (PromoCode),
    CONSTRAINT CK_Promotions_DiscountPercent CHECK (DiscountPercent >= 0 AND DiscountPercent <= 100),
    CONSTRAINT CK_Promotions_MaxDiscount CHECK (MaxDiscountAmount IS NULL OR MaxDiscountAmount > 0),
    CONSTRAINT CK_Promotions_Dates CHECK (EndDate >= StartDate)
);
GO

-- ============================================================================
-- 8. Таблиця зв'язку M:N між Orders та Promotions (OrderPromotions)
-- Забезпечує застосування кількох промокодів або акцій до одного замовлення.
-- ============================================================================
CREATE TABLE dbo.OrderPromotions (
    OrderId             INT NOT NULL,
    PromotionId         INT NOT NULL,
    AppliedDiscount     DECIMAL(18,2) NOT NULL,
    AppliedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_OrderPromotions_AppliedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_OrderPromotions PRIMARY KEY CLUSTERED (OrderId, PromotionId),
    CONSTRAINT FK_OrderPromotions_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderPromotions_Promotions FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions (PromotionId),
    CONSTRAINT CK_OrderPromotions_AppliedDiscount CHECK (AppliedDiscount >= 0)
);
GO

-- ============================================================================
-- ІНДЕКСИ ДЛЯ ОПТИМІЗАЦІЇ ЧАСТИХ БІЗНЕС-ЗАПИТІВ
-- ============================================================================

-- 1. Індекс для швидкої вибірки історії замовлень конкретного клієнта з сортуванням за датою
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_OrderDate
ON dbo.Orders (CustomerId, OrderDate DESC)
INCLUDE (OrderStatus, TotalAmount, IsDeleted);
GO

-- 2. Фільтрований індекс для вибірки активних (не видалених) замовлень за статусом
CREATE NONCLUSTERED INDEX IX_Orders_OrderStatus_Active
ON dbo.Orders (OrderStatus, OrderDate DESC)
WHERE IsDeleted = 0;
GO

-- 3. Індекс для пошуку рядків замовлення
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId_ExternalProductId
ON dbo.OrderItems (OrderId)
INCLUDE (ExternalProductId, ProductSKU, UnitPrice, Quantity, TotalPrice);
GO

-- 4. Індекс для відстеження доставки за ТТН перевізника
CREATE NONCLUSTERED INDEX IX_OrderDeliveries_TrackingNumber
ON dbo.OrderDeliveries (TrackingNumber)
WHERE TrackingNumber IS NOT NULL;
GO

-- 5. Індекс для платіжних транзакцій замовлення
CREATE NONCLUSTERED INDEX IX_PaymentTransactions_OrderId_Status
ON dbo.PaymentTransactions (OrderId, Status)
INCLUDE (Amount, PaymentMethod, PaymentDate);
GO

-- 6. Індекс для історії зміни статусів
CREATE NONCLUSTERED INDEX IX_OrderStatusHistory_OrderId_ChangedAt
ON dbo.OrderStatusHistory (OrderId, ChangedAt DESC);
GO

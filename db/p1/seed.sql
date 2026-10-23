-- Ordering bounded context seed data (Microsoft SQL Server)

SET NOCOUNT ON;

-- Seed customer profiles
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'olexandr.kovalenko@example.com')
BEGIN
    INSERT INTO dbo.Customers (ExternalId, FullName, Email, Phone, CreatedBy)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660001', N'Олександр Коваленко', N'olexandr.kovalenko@example.com', N'+380501112233', N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'iryna.shevchenko@example.com')
BEGIN
    INSERT INTO dbo.Customers (ExternalId, FullName, Email, Phone, CreatedBy)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660002', N'Ірина Шевченко', N'iryna.shevchenko@example.com', N'+380672223344', N'SEED');
END;

-- Seed replicated product catalog cache
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 1)
BEGIN
    INSERT INTO dbo.Products (Id, Sku, Name, Price, IsAvailable, CreatedBy)
    VALUES (1, N'MOT-8100-5W40-5L', N'Моторна олива Motul 8100 X-cess Gen2 5W-40 5л', 1850.00, 1, N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 2)
BEGIN
    INSERT INTO dbo.Products (Id, Sku, Name, Price, IsAvailable, CreatedBy)
    VALUES (2, N'BRM-P85020', N'Комплект передніх гальмівних колодок Brembo P 85 020', 2800.00, 1, N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 3)
BEGIN
    INSERT INTO dbo.Products (Id, Sku, Name, Price, IsAvailable, CreatedBy)
    VALUES (3, N'TOP-ANAM1621', N'Ключ динамометричний Toptul 1/2" 40-210 Нм (ANAM1621)', 3750.00, 1, N'SEED');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 4)
BEGIN
    INSERT INTO dbo.Products (Id, Sku, Name, Price, IsAvailable, CreatedBy)
    VALUES (4, N'BOS-S4-60AH', N'Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A', 4300.00, 1, N'SEED');
END;

-- Seed test order aggregate
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0001')
BEGIN
    DECLARE @CustomerId BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olexandr.kovalenko@example.com');
    DECLARE @OrderId BIGINT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, CreatedBy)
    VALUES (N'ORD-2026-0001', @CustomerId, N'StockConfirmed', 4650.00, N'SEED');

    SET @OrderId = SCOPE_IDENTITY();

    -- 1:1 order delivery details
    INSERT INTO dbo.OrderDetails (
        OrderId, RecipientName, RecipientPhone, ShippingAddress,
        City, PostalCode, ShippingMethod, TrackingNumber, CreatedBy
    )
    VALUES (
        @OrderId, N'Олександр Коваленко', N'+380501112233', N'вул. Антоновича, 120, кв. 45',
        N'Київ', N'03150', N'NovaPoshta', N'20450892019281', N'SEED'
    );

    -- Order items
    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Units, CreatedBy)
    VALUES 
        (@OrderId, 1, N'Моторна олива Motul 8100 X-cess Gen2 5W-40 5л', 1850.00, 1, N'SEED'),
        (@OrderId, 2, N'Комплект передніх гальмівних колодок Brembo P 85 020', 2800.00, 1, N'SEED');

    -- Status history transitions
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, Comment)
    VALUES 
        (@OrderId, NULL, N'Submitted', SYSUTCDATETIME(), N'Order created via web store'),
        (@OrderId, N'Submitted', N'AwaitingValidation', SYSUTCDATETIME(), N'Payment method verified'),
        (@OrderId, N'AwaitingValidation', N'StockConfirmed', SYSUTCDATETIME(), N'Items reserved at Kyiv warehouse');
END;
GO

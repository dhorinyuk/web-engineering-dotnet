-- ============================================================================
-- Проєкт №1: Ідемпотентне наповнення тестовими даними (Seed Data)
-- Контекст замовлень та виконання (Orders & Fulfillment)
-- СУБД: Microsoft SQL Server
-- ============================================================================

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. Клієнти (Customers)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ExternalUserId = 'E5B26E83-9FA3-4D9A-8C21-998877660001')
BEGIN
    INSERT INTO dbo.Customers (ExternalUserId, FirstName, LastName, Email, Phone, CreatedAt)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660001', N'Олександр', N'Коваленко', N'olexandr.kovalenko@example.com', N'+380501112233', '2026-01-10T10:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ExternalUserId = 'E5B26E83-9FA3-4D9A-8C21-998877660002')
BEGIN
    INSERT INTO dbo.Customers (ExternalUserId, FirstName, LastName, Email, Phone, CreatedAt)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660002', N'Ірина', N'Шевченко', N'iryna.shevchenko@example.com', N'+380672223344', '2026-01-12T11:30:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ExternalUserId = 'E5B26E83-9FA3-4D9A-8C21-998877660003')
BEGIN
    INSERT INTO dbo.Customers (ExternalUserId, FirstName, LastName, Email, Phone, CreatedAt)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660003', N'Максим', N'Бондаренко', N'm.bondarenko@example.com', N'+380933334455', '2026-01-15T14:15:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE ExternalUserId = 'E5B26E83-9FA3-4D9A-8C21-998877660004')
BEGIN
    INSERT INTO dbo.Customers (ExternalUserId, FirstName, LastName, Email, Phone, CreatedAt)
    VALUES ('E5B26E83-9FA3-4D9A-8C21-998877660004', N'Сергій', N'Ткаченко', N'serhiy.tkachenko@example.com', N'+380684445566', '2026-01-20T16:40:00');
END;

-- ----------------------------------------------------------------------------
-- 2. Маркетингові акції та промокоди (Promotions)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Promotions WHERE PromoCode = N'AUTOSTART10')
BEGIN
    INSERT INTO dbo.Promotions (PromoCode, DiscountPercent, MaxDiscountAmount, StartDate, EndDate, IsActive, CreatedAt)
    VALUES (N'AUTOSTART10', 10.00, 500.00, '2026-01-01T00:00:00', '2026-12-31T23:59:59', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Promotions WHERE PromoCode = N'SPRINGSERVICE')
BEGIN
    INSERT INTO dbo.Promotions (PromoCode, DiscountPercent, MaxDiscountAmount, StartDate, EndDate, IsActive, CreatedAt)
    VALUES (N'SPRINGSERVICE', 15.00, 800.00, '2026-03-01T00:00:00', '2026-05-31T23:59:59', 1, '2026-02-20T12:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Promotions WHERE PromoCode = N'PROTOOL5')
BEGIN
    INSERT INTO dbo.Promotions (PromoCode, DiscountPercent, MaxDiscountAmount, StartDate, EndDate, IsActive, CreatedAt)
    VALUES (N'PROTOOL5', 5.00, 300.00, '2026-01-01T00:00:00', '2026-12-31T23:59:59', 1, '2026-01-01T00:00:00');
END;

-- ----------------------------------------------------------------------------
-- 3. Замовлення (Orders), Доставки (OrderDeliveries) та Позиції (OrderItems)
-- ----------------------------------------------------------------------------

-- Замовлення 1: Доставлене клієнту (Delivered)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0001')
BEGIN
    DECLARE @CustId1 INT = (SELECT CustomerId FROM dbo.Customers WHERE Email = N'olexandr.kovalenko@example.com');
    DECLARE @Ord1 INT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, OrderStatus, SubTotal, ShippingFee, DiscountAmount, TotalAmount, OrderDate, Notes, CreatedAt)
    VALUES (N'ORD-2026-0001', @CustId1, N'Delivered', 2450.00, 100.00, 245.00, 2305.00, '2026-02-01T10:15:00', N'Передзвонити за годину до доставки', '2026-02-01T10:15:00');

    SET @Ord1 = SCOPE_IDENTITY();

    -- Позиції замовлення 1
    INSERT INTO dbo.OrderItems (OrderId, ExternalProductId, ProductSKU, ProductName, UnitPrice, Quantity, CreatedAt)
    VALUES 
        (@Ord1, 1, N'MOT-8100-5W40-5L', N'Моторна олива Motul 8100 X-cess Gen2 5W-40 5л', 1850.00, 1, '2026-02-01T10:15:00'),
        (@Ord1, 5, N'FLT-MAN-W712', N'Масляний фільтр MANN-FILTER W 712/94', 600.00, 1, '2026-02-01T10:15:00');

    -- Доставка 1
    INSERT INTO dbo.OrderDeliveries (OrderId, Carrier, TrackingNumber, RecipientFullName, RecipientPhone, City, PostalCode, DeliveryAddress, DeliveryStatus, DispatchedAt, DeliveredAt, CreatedAt)
    VALUES (@Ord1, N'Нова Пошта', N'20450892019281', N'Олександр Коваленко', N'+380501112233', N'Київ', N'02000', N'Відділення №15 (вул. Антоновича, 120)', N'Delivered', '2026-02-01T16:00:00', '2026-02-03T11:45:00', '2026-02-01T10:15:00');

    -- Оплата 1
    INSERT INTO dbo.PaymentTransactions (OrderId, TransactionReference, PaymentMethod, Amount, Currency, Status, PaymentDate, CreatedAt)
    VALUES (@Ord1, N'TXN-PAY-20260201-0001', N'CreditCard', 2305.00, N'UAH', N'Success', '2026-02-01T10:18:00', '2026-02-01T10:18:00');

    -- Аудит статусів
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Reason)
    VALUES 
        (@Ord1, NULL, N'Pending', '2026-02-01T10:15:00', N'CUSTOMER', N'Оформлення замовлення'),
        (@Ord1, N'Pending', N'Paid', '2026-02-01T10:18:00', N'PAYMENT_GATEWAY', N'Оплата карткою підтверджена'),
        (@Ord1, N'Paid', N'Processing', '2026-02-01T11:00:00', N'WAREHOUSE_SYS', N'Замовлення передано на комплектацію'),
        (@Ord1, N'Processing', N'Shipped', '2026-02-01T16:00:00', N'WAREHOUSE_SYS', N'Передано кур''єру Нової Пошти'),
        (@Ord1, N'Shipped', N'Delivered', '2026-02-03T11:45:00', N'DELIVERY_SERVICE', N'Успішно вручено отримувачу');

    -- Застосований промокод
    DECLARE @Promo1 INT = (SELECT PromotionId FROM dbo.Promotions WHERE PromoCode = N'AUTOSTART10');
    IF @Promo1 IS NOT NULL
    BEGIN
        INSERT INTO dbo.OrderPromotions (OrderId, PromotionId, AppliedDiscount, AppliedAt)
        VALUES (@Ord1, @Promo1, 245.00, '2026-02-01T10:15:00');
    END;
END;

-- Замовлення 2: Відправлене в дорозі (Shipped)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0002')
BEGIN
    DECLARE @CustId2 INT = (SELECT CustomerId FROM dbo.Customers WHERE Email = N'iryna.shevchenko@example.com');
    DECLARE @Ord2 INT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, OrderStatus, SubTotal, ShippingFee, DiscountAmount, TotalAmount, OrderDate, Notes, CreatedAt)
    VALUES (N'ORD-2026-0002', @CustId2, N'Shipped', 2800.00, 120.00, 0.00, 2920.00, '2026-02-14T09:00:00', N'Колодки з датчиком зносу', '2026-02-14T09:00:00');

    SET @Ord2 = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ExternalProductId, ProductSKU, ProductName, UnitPrice, Quantity, CreatedAt)
    VALUES (@Ord2, 2, N'BRM-P85020', N'Комплект передніх гальмівних колодок Brembo P 85 020', 2800.00, 1, '2026-02-14T09:00:00');

    INSERT INTO dbo.OrderDeliveries (OrderId, Carrier, TrackingNumber, RecipientFullName, RecipientPhone, City, PostalCode, DeliveryAddress, DeliveryStatus, DispatchedAt, CreatedAt)
    VALUES (@Ord2, N'Нова Пошта', N'20450892019282', N'Ірина Шевченко', N'+380672223344', N'Львів', N'79000', N'Поштомат №4420 (вул. Стрийська, 45)', N'Dispatched', '2026-02-14T14:30:00', '2026-02-14T09:00:00');

    INSERT INTO dbo.PaymentTransactions (OrderId, TransactionReference, PaymentMethod, Amount, Currency, Status, PaymentDate, CreatedAt)
    VALUES (@Ord2, N'TXN-PAY-20260214-0002', N'ApplePay', 2920.00, N'UAH', N'Success', '2026-02-14T09:05:00', '2026-02-14T09:05:00');

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Reason)
    VALUES 
        (@Ord2, NULL, N'Pending', '2026-02-14T09:00:00', N'CUSTOMER', N'Оформлення замовлення'),
        (@Ord2, N'Pending', N'Paid', '2026-02-14T09:05:00', N'PAYMENT_GATEWAY', N'Оплата ApplePay успішна'),
        (@Ord2, N'Paid', N'Processing', '2026-02-14T10:00:00', N'WAREHOUSE_SYS', N'Упаковано'),
        (@Ord2, N'Processing', N'Shipped', '2026-02-14T14:30:00', N'WAREHOUSE_SYS', N'Відправлено');
END;

-- Замовлення 3: Оплачене та комплектується на складі (Paid / Processing)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0003')
BEGIN
    DECLARE @CustId3 INT = (SELECT CustomerId FROM dbo.Customers WHERE Email = N'm.bondarenko@example.com');
    DECLARE @Ord3 INT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, OrderStatus, SubTotal, ShippingFee, DiscountAmount, TotalAmount, OrderDate, Notes, CreatedAt)
    VALUES (N'ORD-2026-0003', @CustId3, N'Paid', 3750.00, 0.00, 187.50, 3562.50, '2026-02-18T13:45:00', N'Безкоштовна доставка за акцією', '2026-02-18T13:45:00');

    SET @Ord3 = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ExternalProductId, ProductSKU, ProductName, UnitPrice, Quantity, CreatedAt)
    VALUES (@Ord3, 3, N'TOP-ANAM1621', N'Ключ динамометричний Toptul 1/2" 40-210 Нм (ANAM1621)', 3750.00, 1, '2026-02-18T13:45:00');

    INSERT INTO dbo.OrderDeliveries (OrderId, Carrier, TrackingNumber, RecipientFullName, RecipientPhone, City, PostalCode, DeliveryAddress, DeliveryStatus, CreatedAt)
    VALUES (@Ord3, N'Meest Пошта', NULL, N'Максим Бондаренко', N'+380933334455', N'Харків', N'61000', N'вул. Сумська, 10, кв. 14', N'AwaitingDispatch', '2026-02-18T13:45:00');

    INSERT INTO dbo.PaymentTransactions (OrderId, TransactionReference, PaymentMethod, Amount, Currency, Status, PaymentDate, CreatedAt)
    VALUES (@Ord3, N'TXN-PAY-20260218-0003', N'LiqPay', 3562.50, N'UAH', N'Success', '2026-02-18T13:48:00', '2026-02-18T13:48:00');

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Reason)
    VALUES 
        (@Ord3, NULL, N'Pending', '2026-02-18T13:45:00', N'CUSTOMER', N'Оформлення замовлення'),
        (@Ord3, N'Pending', N'Paid', '2026-02-18T13:48:00', N'PAYMENT_GATEWAY', N'Оплату зараховано через LiqPay');

    DECLARE @Promo3 INT = (SELECT PromotionId FROM dbo.Promotions WHERE PromoCode = N'PROTOOL5');
    IF @Promo3 IS NOT NULL
    BEGIN
        INSERT INTO dbo.OrderPromotions (OrderId, PromotionId, AppliedDiscount, AppliedAt)
        VALUES (@Ord3, @Promo3, 187.50, '2026-02-18T13:45:00');
    END;
END;

-- Замовлення 4: Нове замовлення, очікує оплати (Pending)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0004')
BEGIN
    DECLARE @CustId4 INT = (SELECT CustomerId FROM dbo.Customers WHERE Email = N'serhiy.tkachenko@example.com');
    DECLARE @Ord4 INT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, OrderStatus, SubTotal, ShippingFee, DiscountAmount, TotalAmount, OrderDate, Notes, CreatedAt)
    VALUES (N'ORD-2026-0004', @CustId4, N'Pending', 4300.00, 150.00, 0.00, 4450.00, '2026-02-20T17:00:00', N'Оплата при отриманні накладеним платежем', '2026-02-20T17:00:00');

    SET @Ord4 = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ExternalProductId, ProductSKU, ProductName, UnitPrice, Quantity, CreatedAt)
    VALUES (@Ord4, 4, N'BOS-S4-60AH', N'Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A', 4300.00, 1, '2026-02-20T17:00:00');

    INSERT INTO dbo.OrderDeliveries (OrderId, Carrier, TrackingNumber, RecipientFullName, RecipientPhone, City, PostalCode, DeliveryAddress, DeliveryStatus, CreatedAt)
    VALUES (@Ord4, N'Нова Пошта', NULL, N'Сергій Ткаченко', N'+380684445566', N'Одеса', N'65000', N'Відділення №3 (вул. Дальницька, 23/4)', N'AwaitingDispatch', '2026-02-20T17:00:00');

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Reason)
    VALUES (@Ord4, NULL, N'Pending', '2026-02-20T17:00:00', N'CUSTOMER', N'Очікується підтвердження або сплата');
END;

-- Замовлення 5: Скасоване клієнтом (Cancelled)
IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = N'ORD-2026-0005')
BEGIN
    DECLARE @CustId5 INT = (SELECT CustomerId FROM dbo.Customers WHERE Email = N'olexandr.kovalenko@example.com');
    DECLARE @Ord5 INT;

    INSERT INTO dbo.Orders (OrderNumber, CustomerId, OrderStatus, SubTotal, ShippingFee, DiscountAmount, TotalAmount, OrderDate, Notes, CreatedAt)
    VALUES (N'ORD-2026-0005', @CustId5, N'Cancelled', 1200.00, 80.00, 0.00, 1280.00, '2026-02-15T12:00:00', N'Клієнт помилився з діаметром свічок', '2026-02-15T12:00:00');

    SET @Ord5 = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderItems (OrderId, ExternalProductId, ProductSKU, ProductName, UnitPrice, Quantity, CreatedAt)
    VALUES (@Ord5, 6, N'NGK-LFR6AIX', N'Свічки запалювання NGK Laser Iridium (комплект 4 шт.)', 1200.00, 1, '2026-02-15T12:00:00');

    INSERT INTO dbo.OrderDeliveries (OrderId, Carrier, TrackingNumber, RecipientFullName, RecipientPhone, City, PostalCode, DeliveryAddress, DeliveryStatus, CreatedAt)
    VALUES (@Ord5, N'Укрпошта', NULL, N'Олександр Коваленко', N'+380501112233', N'Київ', N'02000', N'вул. Антоновича, 120, кв. 45', N'AwaitingDispatch', '2026-02-15T12:00:00');

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Reason)
    VALUES 
        (@Ord5, NULL, N'Pending', '2026-02-15T12:00:00', N'CUSTOMER', N'Створено'),
        (@Ord5, N'Pending', N'Cancelled', '2026-02-15T12:30:00', N'CUSTOMER', N'Скасовано за бажанням покупця');
END;

PRINT N'[INFO] Ідемпотентні тестові дані для Проєкту №1 успішно завантажені.';
GO

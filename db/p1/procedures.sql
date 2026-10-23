-- ============================================================================
-- Проєкт №1: Збережувані процедури (Stored Procedures)
-- Контекст замовлень та виконання (Orders & Fulfillment)
-- ============================================================================

-- Очищення існуючих процедур перед створенням
IF OBJECT_ID(N'dbo.usp_ChangeOrderStatus', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ChangeOrderStatus;
IF OBJECT_ID(N'dbo.usp_CreateOrder', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_CreateOrder;
IF OBJECT_ID(N'dbo.usp_AddPaymentTransaction', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_AddPaymentTransaction;
IF OBJECT_ID(N'dbo.usp_SoftDeleteOrder', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_SoftDeleteOrder;
IF OBJECT_ID(N'dbo.usp_GetCustomerOrderSummary', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetCustomerOrderSummary;
GO

-- ============================================================================
-- 1. usp_ChangeOrderStatus
-- Ключова транзакційна процедура зміни статусу замовлення.
-- Використовує блокування рядка через UPDLOCK та ROWLOCK для уникнення race conditions.
-- Валідує дозволені бізнес-переходи станів та веде аудит у OrderStatusHistory.
-- Підтримує опціональну перевірку оптимістичної конкурентності через @ExpectedRowVersion.
-- ============================================================================
CREATE PROCEDURE dbo.usp_ChangeOrderStatus
    @OrderId                INT,
    @NewStatus              NVARCHAR(30),
    @ChangedBy              NVARCHAR(100) = N'SYSTEM',
    @Reason                 NVARCHAR(255) = NULL,
    @ExpectedRowVersion     BINARY(8)     = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(30);
    DECLARE @CurrentRowVersion BINARY(8);
    DECLARE @IsDeleted BIT;
    DECLARE @ErrorMessage NVARCHAR(500);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Зчитування поточного стану замовлення з блокуванням UPDLOCK для запобігання втраченим оновленням
        SELECT 
            @CurrentStatus = o.OrderStatus,
            @CurrentRowVersion = o.RowVersion,
            @IsDeleted = o.IsDeleted
        FROM dbo.Orders o WITH (UPDLOCK, ROWLOCK)
        WHERE o.OrderId = @OrderId;

        -- Перевірка існування замовлення
        IF @CurrentStatus IS NULL
        BEGIN
            SET @ErrorMessage = CONCAT(N'Замовлення з ідентифікатором ', @OrderId, N' не знайдено.');
            THROW 50001, @ErrorMessage, 1;
        END

        -- Перевірка на м'яке видалення
        IF @IsDeleted = 1
        BEGIN
            SET @ErrorMessage = CONCAT(N'Замовлення #', @OrderId, N' позначене як видалене. Зміна статусу неможлива.');
            THROW 50002, @ErrorMessage, 1;
        END

        -- 2. Перевірка оптимістичної конкурентності (якщо передано клієнтську версію рядка)
        IF @ExpectedRowVersion IS NOT NULL AND @CurrentRowVersion <> @ExpectedRowVersion
        BEGIN
            THROW 50003, N'Конфлікт паралельного оновлення: замовлення було змінене іншим користувачем або процесом.', 1;
        END

        -- Якщо статус не змінюється - нічого не робимо
        IF @CurrentStatus = @NewStatus
        BEGIN
            COMMIT TRANSACTION;
            RETURN 0;
        END

        -- 3. Валідація переходів кінцевого автомата статусів (State Machine Validation)
        DECLARE @IsValidTransition BIT = 0;

        IF @CurrentStatus = N'Draft' AND @NewStatus IN (N'Pending', N'Cancelled')
            SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = N'Pending' AND @NewStatus IN (N'Paid', N'Cancelled')
            SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = N'Paid' AND @NewStatus IN (N'Processing', N'Cancelled')
            SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = N'Processing' AND @NewStatus IN (N'Shipped', N'Cancelled')
            SET @IsValidTransition = 1;
        ELSE IF @CurrentStatus = N'Shipped' AND @NewStatus = N'Delivered'
            SET @IsValidTransition = 1;
        -- Delivered та Cancelled є кінцевими (Terminal) станами і не можуть переходити в інші
        ELSE
            SET @IsValidTransition = 0;

        IF @IsValidTransition = 0
        BEGIN
            SET @ErrorMessage = CONCAT(
                N'Неприпустимий перехід статусу замовлення #', @OrderId, 
                N': із "', @CurrentStatus, N'" у "', @NewStatus, N'".'
            );
            THROW 50004, @ErrorMessage, 1;
        END

        -- 4. Оновлення статусу замовлення
        UPDATE dbo.Orders
        SET 
            OrderStatus = @NewStatus,
            UpdatedAt   = SYSUTCDATETIME()
        WHERE OrderId = @OrderId;

        -- 5. Синхронізація зі статусом доставки, якщо замовлення відправлено або вручено
        IF @NewStatus = N'Shipped'
        BEGIN
            UPDATE dbo.OrderDeliveries
            SET 
                DeliveryStatus = N'Dispatched',
                DispatchedAt   = SYSUTCDATETIME(),
                UpdatedAt      = SYSUTCDATETIME()
            WHERE OrderId = @OrderId;
        END
        ELSE IF @NewStatus = N'Delivered'
        BEGIN
            UPDATE dbo.OrderDeliveries
            SET 
                DeliveryStatus = N'Delivered',
                DeliveredAt    = SYSUTCDATETIME(),
                UpdatedAt      = SYSUTCDATETIME()
            WHERE OrderId = @OrderId;
        END

        -- 6. Запис аудиту переходу в історію
        INSERT INTO dbo.OrderStatusHistory (
            OrderId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy,
            Reason
        )
        VALUES (
            @OrderId,
            @CurrentStatus,
            @NewStatus,
            SYSUTCDATETIME(),
            @ChangedBy,
            @Reason
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- 2. usp_CreateOrder
-- Транзакційне створення замовлення разом із логістичним знімком адреси.
-- ============================================================================
CREATE PROCEDURE dbo.usp_CreateOrder
    @CustomerId             INT,
    @OrderNumber            NVARCHAR(30),
    @Carrier                NVARCHAR(50),
    @RecipientFullName      NVARCHAR(100),
    @RecipientPhone         NVARCHAR(20),
    @City                   NVARCHAR(100),
    @PostalCode             NVARCHAR(20),
    @DeliveryAddress        NVARCHAR(250),
    @ShippingFee            DECIMAL(18,2) = 0.00,
    @Notes                  NVARCHAR(500) = NULL,
    @NewOrderId             INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CustomerExists BIT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Перевірка валідності клієнта
        SELECT @CustomerExists = 1 
        FROM dbo.Customers 
        WHERE CustomerId = @CustomerId AND IsDeleted = 0;

        IF @CustomerExists IS NULL OR @CustomerExists = 0
        BEGIN
            THROW 50010, N'Вказаного клієнта не існує або його профіль видалено.', 1;
        END

        -- 1. Створення агрегату замовлення у початковому статусі Pending
        INSERT INTO dbo.Orders (
            OrderNumber,
            CustomerId,
            OrderStatus,
            SubTotal,
            ShippingFee,
            DiscountAmount,
            TotalAmount,
            OrderDate,
            Notes,
            CreatedAt
        )
        VALUES (
            @OrderNumber,
            @CustomerId,
            N'Pending',
            0.00,
            @ShippingFee,
            0.00,
            @ShippingFee,
            SYSUTCDATETIME(),
            @Notes,
            SYSUTCDATETIME()
        );

        SET @NewOrderId = SCOPE_IDENTITY();

        -- 2. Створення логістичного знімка доставки (зв'язок 1:1)
        INSERT INTO dbo.OrderDeliveries (
            OrderId,
            Carrier,
            RecipientFullName,
            RecipientPhone,
            City,
            PostalCode,
            DeliveryAddress,
            DeliveryStatus,
            CreatedAt
        )
        VALUES (
            @NewOrderId,
            @Carrier,
            @RecipientFullName,
            @RecipientPhone,
            @City,
            @PostalCode,
            @DeliveryAddress,
            N'AwaitingDispatch',
            SYSUTCDATETIME()
        );

        -- 3. Запис початкового стану в аудит
        INSERT INTO dbo.OrderStatusHistory (
            OrderId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy,
            Reason
        )
        VALUES (
            @NewOrderId,
            NULL,
            N'Pending',
            SYSUTCDATETIME(),
            N'ORDER_CREATION',
            N'Замовлення успішно створене клієнтом'
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- 3. usp_AddPaymentTransaction
-- Транзакційна реєстрація оплати замовлення.
-- При повній сплаті автоматично переводить замовлення у статус Paid.
-- ============================================================================
CREATE PROCEDURE dbo.usp_AddPaymentTransaction
    @OrderId                INT,
    @TransactionReference   NVARCHAR(100),
    @PaymentMethod          NVARCHAR(50),
    @Amount                 DECIMAL(18,2),
    @Currency               NVARCHAR(3) = N'UAH',
    @ErrorMessage           NVARCHAR(500) = NULL,
    @NewPaymentId           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(30);
    DECLARE @TotalAmount DECIMAL(18,2);
    DECLARE @TotalPaidSoFar DECIMAL(18,2);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Отримання замовлення з блокуванням рядка
        SELECT 
            @CurrentStatus = o.OrderStatus,
            @TotalAmount = o.TotalAmount
        FROM dbo.Orders o WITH (UPDLOCK, ROWLOCK)
        WHERE o.OrderId = @OrderId AND o.IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 50020, N'Замовлення для оплати не знайдено або воно видалене.', 1;
        END

        IF @CurrentStatus = N'Cancelled'
        BEGIN
            THROW 50021, N'Неможливо прийняти платіж за скасованим замовленням.', 1;
        END

        -- Реєстрація транзакції
        DECLARE @PaymentStatus NVARCHAR(30) = CASE WHEN @ErrorMessage IS NULL THEN N'Success' ELSE N'Failed' END;

        INSERT INTO dbo.PaymentTransactions (
            OrderId,
            TransactionReference,
            PaymentMethod,
            Amount,
            Currency,
            Status,
            PaymentDate,
            ErrorMessage,
            CreatedAt
        )
        VALUES (
            @OrderId,
            @TransactionReference,
            @PaymentMethod,
            @Amount,
            @Currency,
            @PaymentStatus,
            SYSUTCDATETIME(),
            @ErrorMessage,
            SYSUTCDATETIME()
        );

        SET @NewPaymentId = SCOPE_IDENTITY();

        -- Якщо платіж успішний, підраховуємо суму всіх успішних оплат
        IF @PaymentStatus = N'Success'
        BEGIN
            SELECT @TotalPaidSoFar = ISNULL(SUM(Amount), 0.00)
            FROM dbo.PaymentTransactions
            WHERE OrderId = @OrderId AND Status = N'Success' AND IsDeleted = 0;

            -- Якщо сплачено повну суму і замовлення очікувало оплати, переводимо в Paid
            IF @TotalPaidSoFar >= @TotalAmount AND @CurrentStatus = N'Pending'
            BEGIN
                EXEC dbo.usp_ChangeOrderStatus 
                    @OrderId = @OrderId,
                    @NewStatus = N'Paid',
                    @ChangedBy = N'PAYMENT_GATEWAY',
                    @Reason = N'Повна оплата замовлення отримана від платіжної системи';
            END
        END

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- 4. usp_SoftDeleteOrder
-- Безпечне м'яке видалення замовлення (IsDeleted = 1).
-- Забороняє видалення відправлених (Shipped) або вручених (Delivered) замовлень.
-- ============================================================================
CREATE PROCEDURE dbo.usp_SoftDeleteOrder
    @OrderId    INT,
    @DeletedBy  NVARCHAR(100) = N'ADMIN'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @CurrentStatus = OrderStatus
        FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
        WHERE OrderId = @OrderId AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 50030, N'Замовлення не знайдено або вже видалене.', 1;
        END

        -- Бізнес-правило: не можна видалити виконане або відправлене замовлення
        IF @CurrentStatus IN (N'Shipped', N'Delivered')
        BEGIN
            DECLARE @Err NVARCHAR(200) = CONCAT(
                N'Заборонено видаляти замовлення у статусі "', @CurrentStatus, N'". Дозволене лише повернення.'
            );
            THROW 50031, @Err, 1;
        END

        -- М'яке видалення замовлення, доставки та позицій
        UPDATE dbo.Orders
        SET 
            IsDeleted = 1,
            OrderStatus = CASE WHEN OrderStatus <> N'Cancelled' THEN N'Cancelled' ELSE OrderStatus END,
            UpdatedAt = SYSUTCDATETIME()
        WHERE OrderId = @OrderId;

        UPDATE dbo.OrderDeliveries
        SET 
            IsDeleted = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE OrderId = @OrderId;

        UPDATE dbo.OrderItems
        SET 
            IsDeleted = 1,
            UpdatedAt = SYSUTCDATETIME()
        WHERE OrderId = @OrderId;

        INSERT INTO dbo.OrderStatusHistory (
            OrderId,
            OldStatus,
            NewStatus,
            ChangedAt,
            ChangedBy,
            Reason
        )
        VALUES (
            @OrderId,
            @CurrentStatus,
            N'Cancelled',
            SYSUTCDATETIME(),
            @DeletedBy,
            N'М''яке видалення замовлення'
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- 5. usp_GetCustomerOrderSummary
-- Аналітична вибірка історії замовлень клієнта з агрегатами та пагінацією.
-- ============================================================================
CREATE PROCEDURE dbo.usp_GetCustomerOrderSummary
    @CustomerId     INT,
    @PageNumber     INT = 1,
    @PageSize       INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 SET @PageSize = 10;
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Результуючий набір 1: Загальна аналітика клієнта
    SELECT 
        c.CustomerId,
        c.FirstName,
        c.LastName,
        c.Email,
        c.Phone,
        COUNT(o.OrderId) AS TotalOrdersCount,
        ISNULL(SUM(CASE WHEN o.OrderStatus <> N'Cancelled' THEN o.TotalAmount ELSE 0 END), 0.00) AS TotalSpentAmount,
        MAX(o.OrderDate) AS LastOrderDate
    FROM dbo.Customers c
    LEFT JOIN dbo.Orders o ON c.CustomerId = o.CustomerId AND o.IsDeleted = 0
    WHERE c.CustomerId = @CustomerId AND c.IsDeleted = 0
    GROUP BY c.CustomerId, c.FirstName, c.LastName, c.Email, c.Phone;

    -- Результуючий набір 2: Посторінковий список замовлень із кількістю позицій та статусом доставки
    SELECT 
        o.OrderId,
        o.OrderNumber,
        o.OrderStatus,
        o.TotalAmount,
        o.OrderDate,
        d.Carrier,
        d.TrackingNumber,
        d.DeliveryStatus,
        COUNT(oi.OrderItemId) AS ItemsCount
    FROM dbo.Orders o
    LEFT JOIN dbo.OrderDeliveries d ON o.OrderId = d.OrderId AND d.IsDeleted = 0
    LEFT JOIN dbo.OrderItems oi ON o.OrderId = oi.OrderId AND oi.IsDeleted = 0
    WHERE o.CustomerId = @CustomerId AND o.IsDeleted = 0
    GROUP BY 
        o.OrderId,
        o.OrderNumber,
        o.OrderStatus,
        o.TotalAmount,
        o.OrderDate,
        d.Carrier,
        d.TrackingNumber,
        d.DeliveryStatus
    ORDER BY o.OrderDate DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

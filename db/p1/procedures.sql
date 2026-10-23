-- Ordering bounded context stored procedures (Microsoft SQL Server)

IF OBJECT_ID(N'dbo.usp_ChangeOrderStatus', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_ChangeOrderStatus;
IF OBJECT_ID(N'dbo.usp_CreateOrder', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_CreateOrder;
IF OBJECT_ID(N'dbo.usp_AddOrderItem', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_AddOrderItem;
IF OBJECT_ID(N'dbo.usp_GetOrderById', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetOrderById;
IF OBJECT_ID(N'dbo.usp_SoftDeleteOrder', N'P') IS NOT NULL DROP PROCEDURE dbo.usp_SoftDeleteOrder;
GO

-- Transactional state transition procedure with exclusive row locking
CREATE OR ALTER PROCEDURE dbo.usp_ChangeOrderStatus
    @OrderId BIGINT,
    @NewStatus NVARCHAR(30),
    @Comment NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) = 
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK, ROWLOCK) WHERE Id = @OrderId);

    IF @Status IS NULL
        THROW 50001, 'Order not found', 1;

    IF @NewStatus = 'StockConfirmed' AND @Status NOT IN ('Submitted', 'AwaitingValidation')
        THROW 50002, 'Stock can be confirmed only for a new order', 1;

    IF @NewStatus = 'Paid' AND @Status <> 'StockConfirmed'
        THROW 50002, 'Only stock-confirmed orders can be paid', 1;

    IF @NewStatus = 'Shipped' AND @Status <> 'Paid'
        THROW 50002, 'Only paid orders can be shipped', 1;

    IF @NewStatus = 'Cancelled' AND @Status IN ('Paid', 'Shipped')
        THROW 50002, 'Cannot cancel paid or shipped order', 1;

    UPDATE dbo.Orders
    SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, Comment)
    VALUES (@OrderId, @Status, @NewStatus, SYSUTCDATETIME(), @Comment);

    COMMIT TRANSACTION;
END;
GO

-- Transactional creation of order aggregate with 1:1 delivery details
CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId     BIGINT,
    @OrderNumber    NVARCHAR(32),
    @RecipientName  NVARCHAR(100),
    @RecipientPhone NVARCHAR(30),
    @ShippingAddress NVARCHAR(255),
    @City           NVARCHAR(100),
    @PostalCode     NVARCHAR(20),
    @ShippingMethod NVARCHAR(50),
    @CreatedBy      NVARCHAR(100) = N'SYSTEM',
    @NewOrderId     BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
        BEGIN
            THROW 50010, N'Customer does not exist or has been soft-deleted.', 1;
        END

        INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, CreatedAt, CreatedBy)
        VALUES (@OrderNumber, @CustomerId, N'Submitted', 0.00, SYSUTCDATETIME(), @CreatedBy);

        SET @NewOrderId = SCOPE_IDENTITY();

        INSERT INTO dbo.OrderDetails (
            OrderId, RecipientName, RecipientPhone, ShippingAddress,
            City, PostalCode, ShippingMethod, CreatedAt, CreatedBy
        )
        VALUES (
            @NewOrderId, @RecipientName, @RecipientPhone, @ShippingAddress,
            @City, @PostalCode, @ShippingMethod, SYSUTCDATETIME(), @CreatedBy
        );

        INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, Comment)
        VALUES (@NewOrderId, NULL, N'Submitted', SYSUTCDATETIME(), N'Order placed by customer');

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

-- Add order line item with price snapshotting and aggregate total recalculation
CREATE OR ALTER PROCEDURE dbo.usp_AddOrderItem
    @OrderId    BIGINT,
    @ProductId  BIGINT,
    @Units      INT,
    @CreatedBy  NVARCHAR(100) = N'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ProductName NVARCHAR(200);
    DECLARE @UnitPrice DECIMAL(18,2);
    DECLARE @IsAvailable BIT;
    DECLARE @OrderStatus NVARCHAR(32);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @OrderStatus = Status
        FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId AND IsDeleted = 0;

        IF @OrderStatus IS NULL
        BEGIN
            THROW 50020, N'Target order was not found or is inactive.', 1;
        END

        IF @OrderStatus NOT IN (N'Submitted', N'AwaitingValidation')
        BEGIN
            THROW 50021, N'Items can only be added while order is in Submitted or AwaitingValidation status.', 1;
        END

        -- Read product data snapshot from local product cache
        SELECT 
            @ProductName = Name,
            @UnitPrice = Price,
            @IsAvailable = IsAvailable
        FROM dbo.Products
        WHERE Id = @ProductId AND IsDeleted = 0;

        IF @ProductName IS NULL
        BEGIN
            THROW 50022, N'Requested product was not found in catalog cache.', 1;
        END

        IF @IsAvailable = 0
        BEGIN
            THROW 50023, N'Requested product is currently marked as unavailable.', 1;
        END

        -- Insert or increment line item units
        IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId AND ProductId = @ProductId)
        BEGIN
            UPDATE dbo.OrderItems
            SET 
                Units = Units + @Units,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedBy = @CreatedBy
            WHERE OrderId = @OrderId AND ProductId = @ProductId;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Units, CreatedAt, CreatedBy)
            VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Units, SYSUTCDATETIME(), @CreatedBy);
        END

        -- Recalculate order aggregate total amount
        UPDATE dbo.Orders
        SET 
            TotalAmount = (SELECT ISNULL(SUM(SubTotal), 0.00) FROM dbo.OrderItems WHERE OrderId = @OrderId AND IsDeleted = 0),
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @CreatedBy
        WHERE Id = @OrderId;

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

-- Query order aggregate with 1:1 delivery details and line items
CREATE OR ALTER PROCEDURE dbo.usp_GetOrderById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Order header and 1:1 delivery details
    SELECT 
        o.Id AS OrderId,
        o.OrderNumber,
        o.CustomerId,
        c.FullName AS CustomerName,
        c.Email AS CustomerEmail,
        o.Status,
        o.TotalAmount,
        o.RowVersion,
        o.CreatedAt,
        d.RecipientName,
        d.RecipientPhone,
        d.ShippingAddress,
        d.City,
        d.PostalCode,
        d.ShippingMethod,
        d.TrackingNumber,
        d.DeliveredAt
    FROM dbo.Orders o
    INNER JOIN dbo.Customers c ON o.CustomerId = c.Id
    LEFT JOIN dbo.OrderDetails d ON o.Id = d.OrderId
    WHERE o.Id = @OrderId AND o.IsDeleted = 0;

    -- Order line items
    SELECT 
        oi.ProductId,
        oi.ProductName,
        oi.UnitPrice,
        oi.Units,
        oi.SubTotal
    FROM dbo.OrderItems oi
    WHERE oi.OrderId = @OrderId AND oi.IsDeleted = 0
    ORDER BY oi.ProductId ASC;
END;
GO

-- Soft-delete order and associated child entities
CREATE OR ALTER PROCEDURE dbo.usp_SoftDeleteOrder
    @OrderId    BIGINT,
    @UpdatedBy  NVARCHAR(100) = N'SYSTEM'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CurrentStatus NVARCHAR(32);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @CurrentStatus = Status
        FROM dbo.Orders WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @OrderId AND IsDeleted = 0;

        IF @CurrentStatus IS NULL
        BEGIN
            THROW 50030, N'Order not found or already deleted.', 1;
        END

        IF @CurrentStatus = N'Shipped'
        BEGIN
            THROW 50031, N'Shipped orders cannot be deleted; return workflow must be initiated instead.', 1;
        END

        UPDATE dbo.Orders
        SET 
            IsDeleted = 1,
            Status = N'Cancelled',
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @UpdatedBy
        WHERE Id = @OrderId;

        UPDATE dbo.OrderDetails
        SET 
            IsDeleted = 1,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @UpdatedBy
        WHERE OrderId = @OrderId;

        UPDATE dbo.OrderItems
        SET 
            IsDeleted = 1,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @UpdatedBy
        WHERE OrderId = @OrderId;

        INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedAt, Comment)
        VALUES (@OrderId, @CurrentStatus, N'Cancelled', SYSUTCDATETIME(), @UpdatedBy);

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

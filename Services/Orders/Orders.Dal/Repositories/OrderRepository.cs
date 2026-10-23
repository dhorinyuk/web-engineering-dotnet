using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Dal.Repositories;

public class OrderRepository : BaseDapperRepository<Order>, IOrderRepository
{
    private const string OrdersTableName = "dbo.Orders";

    public OrderRepository(SqlConnection connection, Func<SqlTransaction?> currentTransactionProvider)
        : base(connection, currentTransactionProvider, OrdersTableName)
    {
    }

    public override async Task<long> AddAsync(Order entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, CreatedAt, CreatedBy, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@OrderNumber, @CustomerId, @Status, @TotalAmount, @CreatedAt, @CreatedBy, @IsDeleted);";

        var cmd = new CommandDefinition(
            sql,
            new
            {
                entity.OrderNumber,
                entity.CustomerId,
                entity.Status,
                entity.TotalAmount,
                entity.CreatedAt,
                entity.CreatedBy,
                entity.IsDeleted
            },
            transaction: Transaction,
            cancellationToken: ct
        );

        try
        {
            var id = await Connection.ExecuteScalarAsync<long>(cmd);
            entity.Id = id;
            return id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Order with order number '{entity.OrderNumber}' already exists.");
        }
    }

    public async Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT 
                o.Id, o.OrderNumber, o.CustomerId, o.Status, o.TotalAmount, o.CreatedAt, o.CreatedBy, o.UpdatedAt, o.UpdatedBy, o.IsDeleted, o.RowVersion,
                i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Units, i.SubTotal, i.CreatedAt, i.CreatedBy, i.UpdatedAt, i.UpdatedBy, i.IsDeleted
            FROM dbo.Orders o
            LEFT JOIN dbo.OrderItems i ON o.Id = i.OrderId AND i.IsDeleted = 0
            WHERE o.Id = @OrderId AND o.IsDeleted = 0;";

        var orderDict = new Dictionary<long, Order>();

        var cmd = new CommandDefinition(sql, new { OrderId = orderId }, transaction: Transaction, cancellationToken: ct);

        await Connection.QueryAsync<Order, OrderItem, Order>(
            cmd,
            (order, item) =>
            {
                if (!orderDict.TryGetValue(order.Id, out var currentOrder))
                {
                    currentOrder = order;
                    currentOrder.Items = new List<OrderItem>();
                    orderDict.Add(currentOrder.Id, currentOrder);
                }

                if (item != null && item.ProductId > 0)
                {
                    currentOrder.Items.Add(item);
                }

                return currentOrder;
            },
            splitOn: "OrderId"
        );

        return orderDict.GetValueOrDefault(orderId);
    }

    public async Task ConfirmAsync(long orderId, CancellationToken ct = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OrderId", orderId, DbType.Int64);
        parameters.Add("@NewStatus", "StockConfirmed", DbType.String, size: 30);
        parameters.Add("@Comment", "Confirmed via OrderService", DbType.String, size: 500);

        var cmd = new CommandDefinition(
            "dbo.usp_ChangeOrderStatus",
            parameters,
            transaction: Transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct
        );

        try
        {
            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            throw new NotFoundException(nameof(Order), orderId);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new BusinessConflictException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Unique constraint violation: {ex.Message}");
        }
    }

    public async Task AddOrderItemAsync(OrderItem item, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Units, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Units, @CreatedAt, @CreatedBy, 0);";

        var cmd = new CommandDefinition(
            sql,
            new
            {
                item.OrderId,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Units,
                item.CreatedAt,
                item.CreatedBy
            },
            transaction: Transaction,
            cancellationToken: ct
        );

        try
        {
            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Order item with product {item.ProductId} already exists in order {item.OrderId}.");
        }
    }

    public async Task AddOrderDetailsAsync(OrderDetails details, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.OrderDetails (OrderId, RecipientName, RecipientPhone, ShippingAddress, City, PostalCode, ShippingMethod, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@OrderId, @RecipientName, @RecipientPhone, @ShippingAddress, @City, @PostalCode, @ShippingMethod, @CreatedAt, @CreatedBy, 0);";

        var cmd = new CommandDefinition(
            sql,
            new
            {
                details.OrderId,
                details.RecipientName,
                details.RecipientPhone,
                details.ShippingAddress,
                details.City,
                details.PostalCode,
                details.ShippingMethod,
                details.CreatedAt,
                details.CreatedBy
            },
            transaction: Transaction,
            cancellationToken: ct
        );

        try
        {
            await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Order details for order {details.OrderId} already exist.");
        }
    }
}

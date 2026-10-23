using Dapper;
using Microsoft.Data.SqlClient;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Dal.Repositories;

public class ProductRepository : BaseDapperRepository<Product>, IProductRepository
{
    private const string ProductsTableName = "dbo.Products";

    public ProductRepository(SqlConnection connection, Func<SqlTransaction?> currentTransactionProvider)
        : base(connection, currentTransactionProvider, ProductsTableName)
    {
    }

    public override async Task<long> AddAsync(Product entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Products (Id, Sku, Name, Price, Stock, IsAvailable, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@Id, @Sku, @Name, @Price, @Stock, @IsAvailable, @CreatedAt, @CreatedBy, @IsDeleted);";

        var cmd = new CommandDefinition(
            sql,
            new
            {
                entity.Id,
                entity.Sku,
                entity.Name,
                entity.Price,
                entity.Stock,
                entity.IsAvailable,
                entity.CreatedAt,
                entity.CreatedBy,
                entity.IsDeleted
            },
            transaction: Transaction,
            cancellationToken: ct
        );

        try
        {
            await Connection.ExecuteAsync(cmd);
            return entity.Id;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new BusinessConflictException($"Product with ID {entity.Id} or SKU '{entity.Sku}' already exists.");
        }
    }

    public async Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Products
            SET Stock = Stock - @Quantity, UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @ProductId AND Stock >= @Quantity AND IsAvailable = 1;";

        var cmd = new CommandDefinition(
            sql,
            new { ProductId = productId, Quantity = quantity },
            transaction: Transaction,
            cancellationToken: ct
        );

        int affected;
        try
        {
            affected = await Connection.ExecuteAsync(cmd);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            throw new NotFoundException(nameof(Product), productId);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new BusinessConflictException(ex.Message);
        }

        if (affected == 0)
        {
            throw new BusinessConflictException($"Insufficient stock or unavailable product for product ID {productId}.");
        }
    }
}

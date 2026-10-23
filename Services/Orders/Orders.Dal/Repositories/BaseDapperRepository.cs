using Dapper;
using Microsoft.Data.SqlClient;

namespace Orders.Dal.Repositories;

/// <summary>
/// Узагальнений репозиторій на базі Dapper з безпечною параметризацією.
/// </summary>
public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    protected readonly string TableName;

    protected BaseDapperRepository(
        SqlConnection connection,
        Func<SqlTransaction?> currentTransactionProvider,
        string tableName)
        : base(connection, currentTransactionProvider)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null or whitespace.", nameof(tableName));

        TableName = tableName;
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id AND IsDeleted = 0";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: Transaction, cancellationToken: ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(cmd);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM {TableName} WHERE IsDeleted = 0";
        var cmd = new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct);
        var result = await Connection.QueryAsync<T>(cmd);
        return result.AsList();
    }

    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var sql = $"UPDATE {TableName} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id";
        var cmd = new CommandDefinition(sql, new { Id = id }, transaction: Transaction, cancellationToken: ct);
        var affected = await Connection.ExecuteAsync(cmd);
        return affected > 0;
    }
}

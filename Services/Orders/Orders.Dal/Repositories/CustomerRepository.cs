using System.Data;
using Microsoft.Data.SqlClient;
using Orders.Domain.Entities;

namespace Orders.Dal.Repositories;

/// <summary>
/// Репозиторій клієнтів, реалізований на чистому ADO.NET без Dapper.
/// Використовує виключно параметризовані SqlCommand, SqlParameter та ручне вичитування SqlDataReader.
/// </summary>
public class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransactionProvider)
        : base(connection, currentTransactionProvider)
    {
    }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, ExternalId, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
            FROM dbo.Customers
            WHERE Id = @Id AND IsDeleted = 0;";

        await using var cmd = new SqlCommand(sql, Connection, Transaction);
        cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, ExternalId, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
            FROM dbo.Customers
            WHERE Email = @Email AND IsDeleted = 0;";

        await using var cmd = new SqlCommand(sql, Connection, Transaction);
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = email });

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, ExternalId, FullName, Email, Phone, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion
            FROM dbo.Customers
            WHERE IsDeleted = 0;";

        await using var cmd = new SqlCommand(sql, Connection, Transaction);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Customer>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapCustomer(reader));
        }

        return list;
    }

    public async Task<long> AddAsync(Customer customer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.Customers (ExternalId, FullName, Email, Phone, CreatedAt, CreatedBy, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@ExternalId, @FullName, @Email, @Phone, @CreatedAt, @CreatedBy, @IsDeleted);";

        await using var cmd = new SqlCommand(sql, Connection, Transaction);
        cmd.Parameters.Add(new SqlParameter("@ExternalId", SqlDbType.UniqueIdentifier) { Value = customer.ExternalId });
        cmd.Parameters.Add(new SqlParameter("@FullName", SqlDbType.NVarChar, 100) { Value = customer.FullName });
        cmd.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = customer.Email });
        cmd.Parameters.Add(new SqlParameter("@Phone", SqlDbType.NVarChar, 30) { Value = customer.Phone });
        cmd.Parameters.Add(new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = customer.CreatedAt });
        cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 100) { Value = customer.CreatedBy });
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", SqlDbType.Bit) { Value = customer.IsDeleted });

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var insertedId = Convert.ToInt64(scalar);
        customer.Id = insertedId;
        return insertedId;
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            ExternalId = reader.GetGuid(reader.GetOrdinal("ExternalId")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Phone = reader.GetString(reader.GetOrdinal("Phone")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
            UpdatedBy = reader.IsDBNull(reader.GetOrdinal("UpdatedBy")) ? null : reader.GetString(reader.GetOrdinal("UpdatedBy")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
            RowVersion = reader.IsDBNull(reader.GetOrdinal("RowVersion")) ? null : (byte[])reader["RowVersion"]
        };
    }
}

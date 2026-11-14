using Microsoft.Data.SqlClient;

namespace Orders.Dal.Repositories;

public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    protected readonly Func<SqlTransaction?> CurrentTransactionProvider;

    protected SqlTransaction? Transaction => CurrentTransactionProvider();

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransactionProvider)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        CurrentTransactionProvider = currentTransactionProvider ?? throw new ArgumentNullException(nameof(currentTransactionProvider));
    }
}

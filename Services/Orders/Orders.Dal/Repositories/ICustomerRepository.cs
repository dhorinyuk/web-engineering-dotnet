using Orders.Domain.Entities;

namespace Orders.Dal.Repositories;

/// <summary>
/// Чистий ADO.NET контракт репозиторію клієнтів.
/// Не наслідує IGenericRepository згідно з архітектурними вимогами.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Customer customer, CancellationToken ct = default);
}

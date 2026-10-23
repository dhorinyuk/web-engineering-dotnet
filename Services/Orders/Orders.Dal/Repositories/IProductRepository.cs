using Orders.Domain.Entities;

namespace Orders.Dal.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task DecreaseStockAsync(long productId, int quantity, CancellationToken ct = default);
}

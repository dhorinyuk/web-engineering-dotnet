using Catalog.Dal.Repositories;

namespace Catalog.Dal;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IProductRepository Products { get; }
    IBrandRepository Brands { get; }
    ICategoryRepository Categories { get; }
    IGenericRepository<T> Repository<T>() where T : class;

    // Single persistence boundary method across all repositories (Criterion 5)
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

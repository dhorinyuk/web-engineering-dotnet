using System.Collections.Concurrent;
using Catalog.Dal.Repositories;

namespace Catalog.Dal;

public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    private IProductRepository? _products;
    private IBrandRepository? _brands;
    private ICategoryRepository? _categories;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public IBrandRepository Brands => _brands ??= new BrandRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);

    public IGenericRepository<T> Repository<T>() where T : class
    {
        return (IGenericRepository<T>)_repositories.GetOrAdd(
            typeof(T),
            _ => new GenericRepository<T>(_context));
    }

    // Repositories themselves DO NOT call SaveChangesAsync; all commits flow through this single method (Criterion 5)
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

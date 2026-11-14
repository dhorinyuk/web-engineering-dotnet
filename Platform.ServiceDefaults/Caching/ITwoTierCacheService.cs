using System;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.ServiceDefaults.Caching;

public interface ITwoTierCacheService
{
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? l1SlidingExpiration = null,
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null,
        CancellationToken ct = default);

    Task RemoveAsync(string key, string? reason = null, CancellationToken ct = default);
    void Remove(string key, string? reason = null);
}

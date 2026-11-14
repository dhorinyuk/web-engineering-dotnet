using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Caching;

public class TwoTierCacheService : ITwoTierCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly CacheMetrics? _metrics;
    private readonly ILogger<TwoTierCacheService>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TwoTierCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        CacheMetrics? metrics = null,
        ILogger<TwoTierCacheService>? logger = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T?>> factory,
        TimeSpan? l1SlidingExpiration = null,
        TimeSpan? l1AbsoluteExpiration = null,
        TimeSpan? l2AbsoluteExpiration = null,
        CancellationToken ct = default)
    {
        // 1. Check L1 MemoryCache
        if (_memoryCache.TryGetValue(key, out T? l1Value) && l1Value != null)
        {
            _metrics?.RecordHit("L1");
            _logger?.LogDebug("Cache L1 HIT for key {Key}", key);
            return l1Value;
        }

        _metrics?.RecordMiss("L1");
        _logger?.LogDebug("Cache L1 MISS for key {Key}", key);

        // 2. Check L2 Redis DistributedCache
        try
        {
            var l2String = await _distributedCache.GetStringAsync(key, ct);
            if (!string.IsNullOrEmpty(l2String))
            {
                var l2Value = JsonSerializer.Deserialize<T>(l2String, JsonOptions);
                if (l2Value != null)
                {
                    _metrics?.RecordHit("L2");
                    _logger?.LogDebug("Cache L2 HIT for key {Key}", key);

                    var l1EntryOptions = new MemoryCacheEntryOptions
                    {
                        Size = 1,
                        SlidingExpiration = l1SlidingExpiration ?? TimeSpan.FromMinutes(2),
                        AbsoluteExpirationRelativeToNow = l1AbsoluteExpiration ?? TimeSpan.FromMinutes(5)
                    };
                    _memoryCache.Set(key, l2Value, l1EntryOptions);

                    return l2Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read from L2 cache for key {Key}: {Message}", key, ex.Message);
        }

        _metrics?.RecordMiss("L2");
        _logger?.LogDebug("Cache L2 MISS for key {Key}", key);

        // 3. Fallback to Database / Factory
        var dbValue = await factory();
        if (dbValue == null)
            return default;

        // 4. Populate L2 and L1 on return path
        try
        {
            var l2EntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = l2AbsoluteExpiration ?? TimeSpan.FromMinutes(30)
            };
            var serialized = JsonSerializer.Serialize(dbValue, JsonOptions);
            await _distributedCache.SetStringAsync(key, serialized, l2EntryOptions, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to write to L2 cache for key {Key}: {Message}", key, ex.Message);
        }

        var memOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = l1SlidingExpiration ?? TimeSpan.FromMinutes(2),
            AbsoluteExpirationRelativeToNow = l1AbsoluteExpiration ?? TimeSpan.FromMinutes(5)
        };
        _memoryCache.Set(key, dbValue, memOptions);

        return dbValue;
    }

    public async Task RemoveAsync(string key, string? reason = null, CancellationToken ct = default)
    {
        _memoryCache.Remove(key);
        try
        {
            await _distributedCache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove key {Key} from L2 cache: {Message}", key, ex.Message);
        }

        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", key, reason ?? "Manual invalidation");
    }

    public void Remove(string key, string? reason = null)
    {
        _memoryCache.Remove(key);
        try
        {
            _distributedCache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove key {Key} from L2 cache synchronously: {Message}", key, ex.Message);
        }

        _logger?.LogInformation("Cache invalidated for key {Key}. Reason: {Reason}", key, reason ?? "Manual invalidation");
    }
}

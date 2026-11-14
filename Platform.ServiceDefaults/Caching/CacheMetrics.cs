using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Platform.ServiceDefaults.Caching;

public class CacheMetrics
{
    private readonly Counter<long> _hitsCounter;
    private readonly Counter<long> _missesCounter;

    public Counter<long> HitsCounter => _hitsCounter;
    public Counter<long> MissesCounter => _missesCounter;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Catalog.Cache");
        _hitsCounter = meter.CreateCounter<long>("cache.hits", description: "Number of cache hits");
        _missesCounter = meter.CreateCounter<long>("cache.misses", description: "Number of cache misses");
    }

    public void RecordHit(string level)
    {
        _hitsCounter.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    }

    public void RecordMiss(string level)
    {
        _missesCounter.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    }
}

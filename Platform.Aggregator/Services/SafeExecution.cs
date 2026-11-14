using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Platform.Aggregator.Services;

public static class SafeExecution
{
    public static async Task<(T? Data, string? Warning)> SafeAsync<T>(
        Func<Task<T>> action,
        string serviceName,
        ILogger logger)
    {
        try
        {
            var result = await action();
            return (result, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aggregator partial degradation: Call to service '{ServiceName}' failed: {Message}", serviceName, ex.Message);
            return (default, $"{serviceName} is unavailable: {ex.Message}");
        }
    }
}

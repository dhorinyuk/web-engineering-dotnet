using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Platform.Aggregator.Services;

public static class SafeExecution
{
    public static async Task<(T? Data, string? Warning)> SafeGrpcAsync<T>(
        Func<Task<T>> action,
        string serviceName,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            logger.LogInformation("{ServiceName} responded successfully via gRPC in {ElapsedMs} ms", serviceName, sw.ElapsedMilliseconds);
            return (result, null);
        }
        catch (RpcException ex)
        {
            sw.Stop();
            var detail = !string.IsNullOrWhiteSpace(ex.Status.Detail) ? ex.Status.Detail : ex.Message;
            logger.LogWarning(ex, "{ServiceName} gRPC call failed with {StatusCode} after {ElapsedMs} ms: {Message}",
                serviceName, ex.StatusCode, sw.ElapsedMilliseconds, detail);
            return (default, $"{serviceName} is unavailable: {detail}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "{ServiceName} failed after {ElapsedMs} ms: {Message}", serviceName, sw.ElapsedMilliseconds, ex.Message);
            return (default, $"{serviceName} is unavailable: {ex.Message}");
        }
    }

    public static async Task<(T? Data, string? Warning)> SafeAsync<T>(
        Func<Task<T>> action,
        string serviceName,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            logger.LogInformation("{ServiceName} responded successfully in {ElapsedMs} ms", serviceName, sw.ElapsedMilliseconds);
            return (result, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "{ServiceName} failed after {ElapsedMs} ms: {Message}", serviceName, sw.ElapsedMilliseconds, ex.Message);
            return (default, $"{serviceName} is unavailable: {ex.Message}");
        }
    }

    public static async Task<T?> SafeAsync<T>(
        Func<Task<T?>> call,
        string serviceName,
        List<string> warnings,
        ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await call();
            sw.Stop();
            logger.LogInformation("{ServiceName} responded successfully in {ElapsedMs} ms", serviceName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "{ServiceName} failed after {ElapsedMs} ms: {Message}", serviceName, sw.ElapsedMilliseconds, ex.Message);
            lock (warnings)
            {
                warnings.Add($"{serviceName} is unavailable: {ex.Message}");
            }
            return default;
        }
    }
}

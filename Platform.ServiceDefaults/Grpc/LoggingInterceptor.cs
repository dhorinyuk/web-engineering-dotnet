using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults.Grpc;

public class LoggingInterceptor : Interceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Method;
        var peer = context.Peer;

        var correlationEntry = context.RequestHeaders.GetValue("x-correlation-id");
        var correlationId = !string.IsNullOrWhiteSpace(correlationEntry)
            ? correlationEntry
            : Guid.NewGuid().ToString("D");

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("Starting gRPC call {Method} from peer {Peer}", method, peer);

            try
            {
                var response = await continuation(request, context);
                sw.Stop();

                var statusCode = context.Status.StatusCode;
                _logger.LogInformation(
                    "Completed gRPC call {Method} from {Peer} with status {StatusCode} in {ElapsedMs} ms",
                    method, peer, statusCode, sw.ElapsedMilliseconds);

                return response;
            }
            catch (RpcException ex)
            {
                sw.Stop();
                _logger.LogWarning(
                    ex,
                    "gRPC call {Method} from {Peer} failed with RpcException {StatusCode} in {ElapsedMs} ms: {Message}",
                    method, peer, ex.StatusCode, sw.ElapsedMilliseconds, ex.Status.Detail ?? ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(
                    ex,
                    "gRPC call {Method} from {Peer} failed with unhandled exception in {ElapsedMs} ms: {Message}",
                    method, peer, sw.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}

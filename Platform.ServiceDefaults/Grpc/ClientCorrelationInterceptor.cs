using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Platform.ServiceDefaults.Correlation;

namespace Platform.ServiceDefaults.Grpc;

public class ClientCorrelationInterceptor : Interceptor
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public ClientCorrelationInterceptor(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var correlationId = _correlationIdProvider.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var headers = context.Options.Headers ?? new Metadata();
            if (headers.GetValue("x-correlation-id") == null &&
                headers.GetValue(CorrelationIdMiddleware.HeaderName) == null)
            {
                headers.Add("x-correlation-id", correlationId);
            }

            var newOptions = context.Options.WithHeaders(headers);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, newOptions);
        }

        return continuation(request, context);
    }
}

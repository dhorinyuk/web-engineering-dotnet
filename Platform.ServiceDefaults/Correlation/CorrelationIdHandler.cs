using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.ServiceDefaults.Correlation;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public CorrelationIdHandler(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdProvider.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

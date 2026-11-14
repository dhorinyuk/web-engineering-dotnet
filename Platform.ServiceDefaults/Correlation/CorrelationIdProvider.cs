namespace Platform.ServiceDefaults.Correlation;

public class CorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}

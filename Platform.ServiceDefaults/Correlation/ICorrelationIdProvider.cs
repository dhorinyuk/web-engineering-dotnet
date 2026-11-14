namespace Platform.ServiceDefaults.Correlation;

public interface ICorrelationIdProvider
{
    string? CorrelationId { get; set; }
}

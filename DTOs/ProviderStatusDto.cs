namespace ZentroAPI.DTOs;

public class UpdateProviderStatusDto
{
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty; // hidden, viewed, negotiating, quoted, assigned, rejected, completed
    public Guid? QuoteId { get; set; }
}

public class ProviderStatusResponseDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid ProviderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public Guid? QuoteId { get; set; }
}


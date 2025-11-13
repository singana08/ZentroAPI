namespace HaluluAPI.DTOs;

public class CreateAgreementDto
{
    public Guid RequestId { get; set; }
    public Guid ProviderId { get; set; }
}

public class AcceptAgreementDto
{
    public Guid AgreementId { get; set; }
}

public class AgreementResponseDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ProviderId { get; set; }
    public bool RequesterAccepted { get; set; }
    public bool ProviderAccepted { get; set; }
    public DateTime? RequesterAcceptedAt { get; set; }
    public DateTime? ProviderAcceptedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
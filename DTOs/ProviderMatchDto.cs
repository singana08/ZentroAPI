namespace ZentroAPI.DTOs;

public class ProviderMatchDto
{
    public Guid ProviderId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public decimal Rating { get; set; }
    public int ExperienceYears { get; set; }
    public string? Bio { get; set; }
    public decimal MatchScore { get; set; }
    public double? Distance { get; set; }
    public string? PricingModel { get; set; }
    public List<string> MatchReasons { get; set; } = new();
}
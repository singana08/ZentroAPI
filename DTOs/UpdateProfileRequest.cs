namespace ZentroAPI.DTOs;

public class UpdateProfileRequest
{
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public string? FullName { get; set; }
    public int? ExperienceYears { get; set; }
    public string? ProfileImage { get; set; }
    public string? PricingModel { get; set; }
    public List<string>? ServiceAreas { get; set; }
    public List<string>? ServiceCategories { get; set; }
}

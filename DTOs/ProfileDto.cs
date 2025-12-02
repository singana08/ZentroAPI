namespace ZentroAPI.DTOs;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string ProfileType { get; set; } = string.Empty; // "REQUESTER" or "PROVIDER"
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // User-level properties
    public bool HasRequesterProfile { get; set; }
    public bool HasProviderProfile { get; set; }
    public bool IsProfileCompleted { get; set; }
    public string? DefaultRole { get; set; }
    
    // Provider specific fields
    public string[]? ServiceCategories { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Bio { get; set; }
    public string[]? ServiceAreas { get; set; }
    public string? PricingModel { get; set; }
    public decimal? Rating { get; set; }
    public decimal? Earnings { get; set; }
    
    // Requester specific fields
    public int? TotalBookings { get; set; }
    public int? CompletedBookings { get; set; }
}

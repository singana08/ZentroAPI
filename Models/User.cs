using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

/// <summary>
/// Simplified User model - core identity only
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();



    [Required]
    [StringLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfileImage { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Required]
    public bool IsEmailVerified { get; set; } = false;

    [Required]
    public bool IsPhoneVerified { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsProfileCompleted { get; set; } = false;

    [StringLength(20)]
    public string DefaultRole { get; set; } = "REQUESTER";

    // Navigation properties
    public Requester? RequesterProfile { get; set; }
    public Provider? ProviderProfile { get; set; }
    public ICollection<OtpRecord> OtpRecords { get; set; } = [];
}
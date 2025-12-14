using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    public DateTime ExpiresAt { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? RevokedAt { get; set; }
    
    [StringLength(100)]
    public string? DeviceId { get; set; }
    
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    
    public User? User { get; set; }
}
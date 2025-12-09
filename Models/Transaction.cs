using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }
    
    public string PaymentIntentId { get; set; } = string.Empty;
    
    public string UserId { get; set; } = string.Empty;
    
    public string JobId { get; set; } = string.Empty;
    
    public string ProviderId { get; set; } = string.Empty;
    
    public long Amount { get; set; }
    
    public string Currency { get; set; } = "inr";
    
    public string Status { get; set; } = string.Empty;
    
    public string? ErrorMessage { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public decimal Quote { get; set; }
    
    public decimal PlatformFee { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public Guid ProfileId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;
    
    public string? Data { get; set; } // JSON string
    
    public bool IsRead { get; set; } = false;
    
    [Required]
    [MaxLength(50)]
    public string NotificationType { get; set; } = string.Empty;
    
    public Guid? RelatedEntityId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    

}
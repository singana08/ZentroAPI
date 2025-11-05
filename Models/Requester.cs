using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HaluluAPI.Models;

/// <summary>
/// Requester profile - users who request services
/// </summary>
[Table("requesters")]
public class Requester
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }



    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZentroAPI.Models;

/// <summary>
/// Tracks the workflow status of service requests with timestamps
/// </summary>
[Table("WorkflowStatuses")]
public class WorkflowStatus
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RequestId { get; set; }

    [Required]
    public Guid ProviderId { get; set; }

    // Assigned Status
    public bool IsAssigned { get; set; } = false;
    public DateTime? AssignedDate { get; set; }

    // In Progress Status
    public bool IsInProgress { get; set; } = false;
    public DateTime? InProgressDate { get; set; }

    // Checked In Status
    public bool IsCheckedIn { get; set; } = false;
    public DateTime? CheckedInDate { get; set; }

    // Completed Status
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedDate { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RequestId))]
    public ServiceRequest? ServiceRequest { get; set; }

    [ForeignKey(nameof(ProviderId))]
    public Provider? Provider { get; set; }
}
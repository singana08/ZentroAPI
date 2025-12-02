using System.ComponentModel.DataAnnotations;

namespace ZentroAPI.DTOs;

/// <summary>
/// DTO for updating workflow status
/// </summary>
public class UpdateWorkflowStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty; // "in_progress", "checked_in", "completed"
}

/// <summary>
/// Response DTO for workflow status
/// </summary>
public class WorkflowStatusResponseDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid ProviderId { get; set; }
    public bool IsAssigned { get; set; }
    public DateTime? AssignedDate { get; set; }
    public bool IsInProgress { get; set; }
    public DateTime? InProgressDate { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

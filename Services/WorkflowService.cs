using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Services;

/// <summary>
/// Service for managing workflow status
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(ApplicationDbContext context, ILogger<WorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, WorkflowStatusResponseDto? Data)> UpdateWorkflowStatusAsync(
        Guid requestId, 
        Guid providerId, 
        string status)
    {
        try
        {
            // Validate status
            var validStatuses = new[] { "in_progress", "checked_in", "completed" };
            if (!validStatuses.Contains(status.ToLower()))
            {
                return (false, "Invalid status. Must be: in_progress, checked_in, or completed", null);
            }

            // Get or create workflow status record
            var workflowStatus = await _context.WorkflowStatuses
                .FirstOrDefaultAsync(ws => ws.RequestId == requestId && ws.ProviderId == providerId);

            if (workflowStatus == null)
            {
                workflowStatus = new WorkflowStatus
                {
                    RequestId = requestId,
                    ProviderId = providerId
                };
                _context.WorkflowStatuses.Add(workflowStatus);
            }

            var now = DateTime.UtcNow;
            workflowStatus.UpdatedAt = now;

            // Update status based on input
            ServiceRequestStatus serviceRequestStatus;
            ProviderStatus providerStatus;
            
            switch (status.ToLower())
            {
                case "in_progress":
                    workflowStatus.IsInProgress = true;
                    workflowStatus.InProgressDate = now;
                    serviceRequestStatus = ServiceRequestStatus.InProgress;
                    providerStatus = ProviderStatus.Assigned;
                    break;
                case "checked_in":
                    workflowStatus.IsCheckedIn = true;
                    workflowStatus.CheckedInDate = now;
                    serviceRequestStatus = ServiceRequestStatus.CheckedIn;
                    providerStatus = ProviderStatus.Assigned;
                    break;
                case "completed":
                    workflowStatus.IsCompleted = true;
                    workflowStatus.CompletedDate = now;
                    serviceRequestStatus = ServiceRequestStatus.Completed;
                    providerStatus = ProviderStatus.Completed;
                    break;
                default:
                    return (false, "Invalid status", null);
            }

            // Update ServiceRequest status
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(sr => sr.Id == requestId);
            
            if (serviceRequest != null)
            {
                serviceRequest.Status = serviceRequestStatus;
                serviceRequest.UpdatedAt = now;
            }

            // Update ProviderRequestStatus
            var providerRequestStatus = await _context.ProviderRequestStatuses
                .FirstOrDefaultAsync(prs => prs.RequestId == requestId && prs.ProviderId == providerId);
            
            if (providerRequestStatus != null)
            {
                providerRequestStatus.Status = providerStatus;
                providerRequestStatus.LastUpdated = now;
            }

            await _context.SaveChangesAsync();

            var responseDto = MapToResponseDto(workflowStatus);
            return (true, "Workflow status updated successfully", responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow status for request {RequestId}, provider {ProviderId}", requestId, providerId);
            return (false, "An error occurred while updating workflow status", null);
        }
    }

    public async Task<(bool Success, string Message, WorkflowStatusResponseDto? Data)> GetWorkflowStatusAsync(
        Guid requestId, 
        Guid providerId)
    {
        try
        {
            var workflowStatus = await _context.WorkflowStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(ws => ws.RequestId == requestId && ws.ProviderId == providerId);

            if (workflowStatus == null)
            {
                return (false, "Workflow status not found", null);
            }

            var responseDto = MapToResponseDto(workflowStatus);
            return (true, "Workflow status retrieved successfully", responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow status for request {RequestId}, provider {ProviderId}", requestId, providerId);
            return (false, "An error occurred while retrieving workflow status", null);
        }
    }

    private static WorkflowStatusResponseDto MapToResponseDto(WorkflowStatus workflowStatus)
    {
        return new WorkflowStatusResponseDto
        {
            Id = workflowStatus.Id,
            RequestId = workflowStatus.RequestId,
            ProviderId = workflowStatus.ProviderId,
            IsAssigned = workflowStatus.IsAssigned,
            AssignedDate = workflowStatus.AssignedDate,
            IsInProgress = workflowStatus.IsInProgress,
            InProgressDate = workflowStatus.InProgressDate,
            IsCheckedIn = workflowStatus.IsCheckedIn,
            CheckedInDate = workflowStatus.CheckedInDate,
            IsCompleted = workflowStatus.IsCompleted,
            CompletedDate = workflowStatus.CompletedDate,
            CreatedAt = workflowStatus.CreatedAt,
            UpdatedAt = workflowStatus.UpdatedAt
        };
    }
}

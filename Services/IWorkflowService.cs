using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

/// <summary>
/// Interface for workflow status management
/// </summary>
public interface IWorkflowService
{
    Task<(bool Success, string Message, WorkflowStatusResponseDto? Data)> UpdateWorkflowStatusAsync(
        Guid requestId, 
        Guid providerId, 
        string status);
        
    Task<(bool Success, string Message, WorkflowStatusResponseDto? Data)> GetWorkflowStatusAsync(
        Guid requestId, 
        Guid providerId);
}
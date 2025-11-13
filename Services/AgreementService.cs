using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

public class AgreementService : IAgreementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgreementService> _logger;

    public AgreementService(ApplicationDbContext context, ILogger<AgreementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AgreementResponseDto? Data)> CreateAgreementAsync(Guid requesterId, CreateAgreementDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            if (serviceRequest.RequesterId != requesterId)
                return (false, "Access denied", null);

            // Check if provider is already assigned
            if (serviceRequest.AssignedProviderId != null)
                return (false, "Provider already assigned to this request", null);

            var existingAgreement = await _context.Agreements
                .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && a.ProviderId == request.ProviderId);
            
            if (existingAgreement != null)
                return (false, "Agreement already exists", null);

            var agreement = new Agreement
            {
                RequestId = request.RequestId,
                RequesterId = requesterId,
                ProviderId = request.ProviderId
            };

            _context.Agreements.Add(agreement);
            await _context.SaveChangesAsync();

            var response = new AgreementResponseDto
            {
                Id = agreement.Id,
                RequestId = agreement.RequestId,
                RequesterId = agreement.RequesterId,
                ProviderId = agreement.ProviderId,
                RequesterAccepted = agreement.RequesterAccepted,
                ProviderAccepted = agreement.ProviderAccepted,
                RequesterAcceptedAt = agreement.RequesterAcceptedAt,
                ProviderAcceptedAt = agreement.ProviderAcceptedAt,
                FinalizedAt = agreement.FinalizedAt,
                Status = agreement.Status.ToString(),
                CreatedAt = agreement.CreatedAt
            };

            return (true, "Agreement created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating agreement");
            return (false, "Failed to create agreement", null);
        }
    }

    public async Task<(bool Success, string Message, AgreementResponseDto? Data)> AcceptAgreementAsync(Guid profileId, AcceptAgreementDto request)
    {
        try
        {
            var agreement = await _context.Agreements.FindAsync(request.AgreementId);
            if (agreement == null)
                return (false, "Agreement not found", null);

            bool isRequester = agreement.RequesterId == profileId;
            bool isProvider = agreement.ProviderId == profileId;

            if (!isRequester && !isProvider)
                return (false, "Access denied", null);

            if (isRequester)
            {
                agreement.RequesterAccepted = true;
                agreement.RequesterAcceptedAt = DateTime.UtcNow;
            }
            else
            {
                agreement.ProviderAccepted = true;
                agreement.ProviderAcceptedAt = DateTime.UtcNow;
            }

            // Check if both parties accepted
            if (agreement.RequesterAccepted && agreement.ProviderAccepted)
            {
                agreement.Status = AgreementStatus.Accepted;
                agreement.FinalizedAt = DateTime.UtcNow;
                
                // Assign provider to service request
                var serviceRequest = await _context.ServiceRequests.FindAsync(agreement.RequestId);
                if (serviceRequest != null)
                {
                    serviceRequest.AssignedProviderId = agreement.ProviderId;
                    serviceRequest.Status = ServiceRequestStatus.Assigned;
                    serviceRequest.UpdatedAt = DateTime.UtcNow;
                }
            }

            agreement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = new AgreementResponseDto
            {
                Id = agreement.Id,
                RequestId = agreement.RequestId,
                RequesterId = agreement.RequesterId,
                ProviderId = agreement.ProviderId,
                RequesterAccepted = agreement.RequesterAccepted,
                ProviderAccepted = agreement.ProviderAccepted,
                RequesterAcceptedAt = agreement.RequesterAcceptedAt,
                ProviderAcceptedAt = agreement.ProviderAcceptedAt,
                FinalizedAt = agreement.FinalizedAt,
                Status = agreement.Status.ToString(),
                CreatedAt = agreement.CreatedAt
            };

            return (true, "Agreement accepted successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting agreement");
            return (false, "Failed to accept agreement", null);
        }
    }

    public async Task<(bool Success, string Message, AgreementResponseDto? Data)> GetAgreementAsync(Guid requestId, Guid profileId)
    {
        try
        {
            var agreement = await _context.Agreements
                .FirstOrDefaultAsync(a => a.RequestId == requestId && 
                                    (a.RequesterId == profileId || a.ProviderId == profileId));

            if (agreement == null)
                return (true, "Agreement not found", null);

            var response = new AgreementResponseDto
            {
                Id = agreement.Id,
                RequestId = agreement.RequestId,
                RequesterId = agreement.RequesterId,
                ProviderId = agreement.ProviderId,
                RequesterAccepted = agreement.RequesterAccepted,
                ProviderAccepted = agreement.ProviderAccepted,
                RequesterAcceptedAt = agreement.RequesterAcceptedAt,
                ProviderAcceptedAt = agreement.ProviderAcceptedAt,
                FinalizedAt = agreement.FinalizedAt,
                Status = agreement.Status.ToString(),
                CreatedAt = agreement.CreatedAt
            };

            return (true, "Agreement retrieved successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreement");
            return (false, "Failed to get agreement", null);
        }
    }

    public async Task<(bool Success, string Message)> CancelAgreementAsync(Guid profileId, AcceptAgreementDto request)
    {
        try
        {
            var agreement = await _context.Agreements.FindAsync(request.AgreementId);
            if (agreement == null)
                return (false, "Agreement not found");

            bool isRequester = agreement.RequesterId == profileId;
            bool isProvider = agreement.ProviderId == profileId;

            if (!isRequester && !isProvider)
                return (false, "Access denied");

            agreement.Status = AgreementStatus.Cancelled;
            agreement.UpdatedAt = DateTime.UtcNow;

            // Remove provider assignment from service request if it was assigned
            var serviceRequest = await _context.ServiceRequests.FindAsync(agreement.RequestId);
            if (serviceRequest != null && serviceRequest.AssignedProviderId == agreement.ProviderId)
            {
                serviceRequest.AssignedProviderId = null;
                serviceRequest.Status = ServiceRequestStatus.Open;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return (true, "Agreement cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling agreement");
            return (false, "Failed to cancel agreement");
        }
    }
}
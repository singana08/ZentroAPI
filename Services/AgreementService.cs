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



    public async Task<(bool Success, string Message, AgreementResponseDto? Data)> RespondToAgreementAsync(Guid profileId, AcceptAgreementDto request)
    {
        try
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(request.RequestId);
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            bool isRequester = serviceRequest.RequesterId == profileId;
            Guid providerId = Guid.Empty;

            if (isRequester)
            {
                // Requester responding - find provider from quotes
                var quote = await _context.Quotes
                    .FirstOrDefaultAsync(q => q.RequestId == request.RequestId);
                if (quote == null)
                    return (false, "No quotes found for this request", null);
                providerId = quote.ProviderId;
            }
            else
            {
                // Provider responding - check if they have a quote
                var quote = await _context.Quotes
                    .FirstOrDefaultAsync(q => q.RequestId == request.RequestId && q.ProviderId == profileId);
                if (quote == null)
                    return (false, "You don't have a quote for this request", null);
                providerId = profileId;
            }

            var agreement = await _context.Agreements
                .FirstOrDefaultAsync(a => a.RequestId == request.RequestId && 
                                    a.RequesterId == serviceRequest.RequesterId && 
                                    a.ProviderId == providerId);

            if (agreement == null)
            {
                agreement = new Agreement
                {
                    RequestId = request.RequestId,
                    RequesterId = serviceRequest.RequesterId,
                    ProviderId = providerId
                };
                _context.Agreements.Add(agreement);
                await _context.SaveChangesAsync();
            }

            if (request.IsAccepted)
            {
                if (isRequester)
                {
                    agreement.RequesterAccepted = true;
                    agreement.RequesterAcceptedAt = DateTime.UtcNow;
                    
                    var message = new Message
                    {
                        SenderId = profileId,
                        ReceiverId = providerId,
                        RequestId = agreement.RequestId,
                        MessageText = "I have agreed to your proposal. Waiting for your agreement to finalize the deal."
                    };
                    _context.Messages.Add(message);
                }
                else
                {
                    agreement.ProviderAccepted = true;
                    agreement.ProviderAcceptedAt = DateTime.UtcNow;
                    
                    var message = new Message
                    {
                        SenderId = profileId,
                        ReceiverId = serviceRequest.RequesterId,
                        RequestId = agreement.RequestId,
                        MessageText = "I have accepted your agreement. Looking forward to working with you!"
                    };
                    _context.Messages.Add(message);
                }
            }
            else
            {
                agreement.Status = AgreementStatus.Rejected;
                agreement.UpdatedAt = DateTime.UtcNow;
                
                var rejectionMessage = new Message
                {
                    SenderId = profileId,
                    ReceiverId = isRequester ? providerId : serviceRequest.RequesterId,
                    RequestId = agreement.RequestId,
                    MessageText = isRequester 
                        ? "I have to decline your proposal. Thank you for your time."
                        : "I cannot accept this agreement at this time. Thank you for considering me."
                };
                _context.Messages.Add(rejectionMessage);
                
                // Hide request from provider when rejected
                var existingStatus = await _context.ProviderRequestStatuses
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.RequestId == agreement.RequestId);
                
                if (existingStatus != null)
                {
                    existingStatus.Status = ProviderStatus.Hidden;
                }
                else
                {
                    var hiddenStatus = new ProviderRequestStatus
                    {
                        ProviderId = providerId,
                        RequestId = agreement.RequestId,
                        Status = ProviderStatus.Hidden
                    };
                    _context.ProviderRequestStatuses.Add(hiddenStatus);
                }
                
                // Set service request back to open
                serviceRequest.Status = ServiceRequestStatus.Open;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                
                agreement.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                var rejectionResponse = new AgreementResponseDto
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
                
                return (true, "Agreement rejected successfully", rejectionResponse);
            }

            // Check if both parties accepted
            if (agreement.RequesterAccepted && agreement.ProviderAccepted)
            {
                agreement.Status = AgreementStatus.Accepted;
                agreement.FinalizedAt = DateTime.UtcNow;
                
                // Assign provider to service request
                serviceRequest.AssignedProviderId = providerId;
                serviceRequest.Status = ServiceRequestStatus.Assigned;
                serviceRequest.UpdatedAt = DateTime.UtcNow;
                
                // Update provider request status to Assigned
                var providerStatus = await _context.ProviderRequestStatuses
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.RequestId == agreement.RequestId);
                
                if (providerStatus != null)
                {
                    providerStatus.Status = ProviderStatus.Assigned;
                }
                else
                {
                    var assignedStatus = new ProviderRequestStatus
                    {
                        ProviderId = providerId,
                        RequestId = agreement.RequestId,
                        Status = ProviderStatus.Assigned
                    };
                    _context.ProviderRequestStatuses.Add(assignedStatus);
                }
                
                // Update quote status to accepted
                var quote = await _context.Quotes
                    .FirstOrDefaultAsync(q => q.RequestId == request.RequestId && q.ProviderId == providerId);
                if (quote != null)
                {
                    quote.Status = "Accepted";
                    quote.UpdatedAt = DateTime.UtcNow;
                }
                
                // Send finalization message
                var finalMessage = new Message
                {
                    SenderId = profileId,
                    ReceiverId = isRequester ? providerId : serviceRequest.RequesterId,
                    RequestId = agreement.RequestId,
                    MessageText = "🎉 Great! Both parties have agreed. The service is now confirmed. Let's get started!"
                };
                _context.Messages.Add(finalMessage);
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




}
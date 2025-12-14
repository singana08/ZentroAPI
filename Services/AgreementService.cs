using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ZentroAPI.Services;

public class AgreementService : IAgreementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgreementService> _logger;
    private readonly INotificationService _notificationService;

    public AgreementService(ApplicationDbContext context, ILogger<AgreementService> logger, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }



    public async Task<(bool Success, string Message, AgreementResponseDto? Data)> RespondToAgreementAsync(Guid profileId, AcceptAgreementDto request)
    {
        try
        {
            // Get quote to extract requestId, providerId, and requesterId
            var quote = await _context.Quotes
                .Include(q => q.ServiceRequest)
                .FirstOrDefaultAsync(q => q.Id == request.QuoteId);
            
            if (quote == null)
                return (false, "Quote not found", null);

            var serviceRequest = quote.ServiceRequest;
            if (serviceRequest == null)
                return (false, "Service request not found", null);

            bool isRequester = serviceRequest.RequesterId == profileId;
            bool isProvider = quote.ProviderId == profileId;
            
            if (!isRequester && !isProvider)
                return (false, "You are not authorized to respond to this agreement", null);

            Guid providerId = quote.ProviderId;
            Guid requesterId = serviceRequest.RequesterId;
            Guid requestId = serviceRequest.Id;

            var agreement = await _context.Agreements
                .FirstOrDefaultAsync(a => a.QuoteId == request.QuoteId && 
                                    a.RequesterId == requesterId && 
                                    a.ProviderId == providerId);

            if (agreement == null)
            {
                agreement = new Agreement
                {
                    QuoteId = request.QuoteId,
                    RequesterId = requesterId,
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
                        RequestId = requestId,
                        MessageText = "I have agreed to your proposal. Waiting for your agreement to finalize the deal."
                    };
                    _context.Messages.Add(message);
                    
                    // Notify provider that requester accepted the quote
                    await _notificationService.NotifyOfQuoteAcceptanceAsync(request.QuoteId, profileId, true);
                }
                else
                {
                    agreement.ProviderAccepted = true;
                    agreement.ProviderAcceptedAt = DateTime.UtcNow;
                    
                    var message = new Message
                    {
                        SenderId = profileId,
                        ReceiverId = requesterId,
                        RequestId = requestId,
                        MessageText = "I have accepted your agreement. Looking forward to working with you!"
                    };
                    _context.Messages.Add(message);
                    
                    // Notify requester that provider accepted the quote
                    await _notificationService.NotifyOfQuoteAcceptanceAsync(request.QuoteId, profileId, false);
                }
            }
            else
            {
                agreement.Status = AgreementStatus.Rejected;
                agreement.UpdatedAt = DateTime.UtcNow;
                
                var rejectionMessage = new Message
                {
                    SenderId = profileId,
                    ReceiverId = isRequester ? providerId : requesterId,
                    RequestId = requestId,
                    MessageText = isRequester 
                        ? "I have to decline your proposal. Thank you for your time."
                        : "I cannot accept this agreement at this time. Thank you for considering me."
                };
                _context.Messages.Add(rejectionMessage);
                
                // Hide request from provider when rejected
                var existingStatus = await _context.ProviderRequestStatuses
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.RequestId == requestId);
                
                if (existingStatus != null)
                {
                    existingStatus.Status = ProviderStatus.Hidden;
                }
                else
                {
                    var hiddenStatus = new ProviderRequestStatus
                    {
                        ProviderId = providerId,
                        RequestId = requestId,
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
                    QuoteId = agreement.QuoteId,
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
                    .FirstOrDefaultAsync(p => p.ProviderId == providerId && p.RequestId == requestId);
                
                if (providerStatus != null)
                {
                    providerStatus.Status = ProviderStatus.Assigned;
                }
                else
                {
                    var assignedStatus = new ProviderRequestStatus
                    {
                        ProviderId = providerId,
                        RequestId = requestId,
                        Status = ProviderStatus.Assigned
                    };
                    _context.ProviderRequestStatuses.Add(assignedStatus);
                }
                
                // Update quote status to accepted
                quote.Status = "Accepted";
                quote.UpdatedAt = DateTime.UtcNow;

                
                // Send finalization message
                var finalMessage = new Message
                {
                    SenderId = profileId,
                    ReceiverId = isRequester ? providerId : requesterId,
                    RequestId = requestId,
                    MessageText = "🎉 Great! Both parties have agreed. The service is now confirmed. Let's get started!"
                };
                _context.Messages.Add(finalMessage);
            }

            agreement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = new AgreementResponseDto
            {
                Id = agreement.Id,
                QuoteId = agreement.QuoteId,
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
            // Find agreement by quote that belongs to the request
            var quote = await _context.Quotes
                .FirstOrDefaultAsync(q => q.RequestId == requestId);
            
            if (quote == null)
                return (true, "No quotes found for this request", null);

            var agreement = await _context.Agreements
                .FirstOrDefaultAsync(a => a.QuoteId == quote.Id && 
                                    (a.RequesterId == profileId || a.ProviderId == profileId));

            if (agreement == null)
                return (true, "Agreement not found", null);

            var response = new AgreementResponseDto
            {
                Id = agreement.Id,
                QuoteId = agreement.QuoteId,
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

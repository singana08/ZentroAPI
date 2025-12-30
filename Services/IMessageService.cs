using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IMessageService
{
    Task<(bool Success, string Message, MessageResponseDto? Data)> SendMessageAsync(Guid senderId, SendMessageDto request);
    Task<(bool Success, string Message, MessagesListResponse? Data)> GetMessagesAsync(Guid requestId, Guid profileId, Guid otherUserId);

    Task<(bool Success, string Message)> ReopenRequestAsync(Guid requesterId, RequestActionDto request);
    Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetOpenRequestsAsync(Guid providerId, int page, int pageSize);
    Task<(bool Success, string Message, PaginatedServiceRequestsDto? Data)> GetProviderJobsAsync(Guid providerId, int page, int pageSize);
    Task<(bool Success, string Message, ProviderStatusResponseDto? Data)> UpdateProviderStatusAsync(Guid providerId, UpdateProviderStatusDto request);
    Task<(bool Success, string Message, ProviderStatusResponseDto? Data)> GetProviderStatusAsync(Guid providerId, Guid requestId);
    Task<(bool Success, string Message)> CompleteRequestAsync(Guid providerId, RequestActionDto request);
    Task<(bool Success, string Message, ChatListResponse? Data)> GetChatListAsync(Guid profileId);
    Task<(bool Success, string Message)> MarkMessagesAsReadAsync(Guid requestId, Guid profileId);
    Task<(bool Success, string Message)> MarkMessageAsDeliveredAsync(Guid messageId);
}

using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public interface IReferralService
{
    Task<(bool Success, string Message, ReferralCodeDto? Data)> GetUserReferralCodeAsync(Guid userId);
    Task<(bool Success, string Message)> UseReferralCodeAsync(Guid userId, string referralCode);
    Task<(bool Success, string Message, ReferralStatsDto? Data)> GetReferralStatsAsync(Guid userId);
    Task ProcessReferralCompletionAsync(Guid userId);
}
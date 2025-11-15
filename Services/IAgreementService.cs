using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

public interface IAgreementService
{
    Task<(bool Success, string Message, AgreementResponseDto? Data)> RespondToAgreementAsync(Guid profileId, AcceptAgreementDto request);
    Task<(bool Success, string Message, AgreementResponseDto? Data)> GetAgreementAsync(Guid requestId, Guid profileId);
}
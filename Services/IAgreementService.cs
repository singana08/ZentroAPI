using HaluluAPI.DTOs;

namespace HaluluAPI.Services;

public interface IAgreementService
{
    Task<(bool Success, string Message, AgreementResponseDto? Data)> CreateAgreementAsync(Guid requesterId, CreateAgreementDto request);
    Task<(bool Success, string Message, AgreementResponseDto? Data)> AcceptAgreementAsync(Guid profileId, AcceptAgreementDto request);
    Task<(bool Success, string Message, AgreementResponseDto? Data)> GetAgreementAsync(Guid requestId, Guid profileId);
    Task<(bool Success, string Message)> CancelAgreementAsync(Guid profileId, AcceptAgreementDto request);
}
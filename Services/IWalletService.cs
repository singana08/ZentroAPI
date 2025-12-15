using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public interface IWalletService
{
    Task<(bool Success, string Message, WalletDto? Data)> GetWalletAsync(Guid userId);
    Task<(bool Success, string Message)> AddCreditAsync(Guid userId, decimal amount, TransactionSource source, string description, Guid? referenceId = null, DateTime? expiresAt = null);
    Task<(bool Success, string Message)> DebitAsync(Guid userId, decimal amount, TransactionSource source, string description, Guid? referenceId = null);
    Task ProcessExpiredCreditsAsync();
}
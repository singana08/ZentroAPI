using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public class WalletService : IWalletService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(ApplicationDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, WalletDto? Data)> GetWalletAsync(Guid userId)
    {
        try
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            
            var transactions = await _context.Set<WalletTransaction>()
                .Where(wt => wt.WalletId == wallet.Id)
                .OrderByDescending(wt => wt.CreatedAt)
                .Take(20)
                .Select(wt => new WalletTransactionDto
                {
                    Id = wt.Id,
                    Type = wt.Type.ToString(),
                    Source = wt.Source.ToString(),
                    Amount = wt.Amount,
                    BalanceAfter = wt.BalanceAfter,
                    Description = wt.Description,
                    ExpiresAt = wt.ExpiresAt,
                    CreatedAt = wt.CreatedAt
                })
                .ToListAsync();

            var walletDto = new WalletDto
            {
                Id = wallet.Id,
                Balance = wallet.Balance,
                RecentTransactions = transactions
            };

            return (true, "Wallet retrieved successfully", walletDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet for user {UserId}", userId);
            return (false, "Failed to get wallet", null);
        }
    }

    public async Task<(bool Success, string Message)> AddCreditAsync(Guid userId, decimal amount, TransactionSource source, string description, Guid? referenceId = null, DateTime? expiresAt = null)
    {
        try
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            
            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Credit,
                Source = source,
                Amount = amount,
                BalanceAfter = wallet.Balance,
                Description = description,
                ReferenceId = referenceId,
                ExpiresAt = expiresAt
            };

            _context.Set<WalletTransaction>().Add(transaction);
            await _context.SaveChangesAsync();

            return (true, "Credit added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding credit to wallet for user {UserId}", userId);
            return (false, "Failed to add credit");
        }
    }

    public async Task<(bool Success, string Message)> DebitAsync(Guid userId, decimal amount, TransactionSource source, string description, Guid? referenceId = null)
    {
        try
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            
            if (wallet.Balance < amount)
                return (false, "Insufficient balance");

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Debit,
                Source = source,
                Amount = amount,
                BalanceAfter = wallet.Balance,
                Description = description,
                ReferenceId = referenceId
            };

            _context.Set<WalletTransaction>().Add(transaction);
            await _context.SaveChangesAsync();

            return (true, "Debit processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debiting wallet for user {UserId}", userId);
            return (false, "Failed to process debit");
        }
    }

    public async Task ProcessExpiredCreditsAsync()
    {
        try
        {
            var expiredTransactions = await _context.Set<WalletTransaction>()
                .Include(wt => wt.Wallet)
                .Where(wt => wt.Type == TransactionType.Credit 
                           && wt.ExpiresAt.HasValue 
                           && wt.ExpiresAt.Value <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var transaction in expiredTransactions)
            {
                // Create expiry debit transaction
                var expiryTransaction = new WalletTransaction
                {
                    WalletId = transaction.WalletId,
                    Type = TransactionType.Debit,
                    Source = TransactionSource.Expiry,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.Wallet.Balance - transaction.Amount,
                    Description = $"Expired credit from {transaction.CreatedAt:yyyy-MM-dd}",
                    ReferenceId = transaction.Id
                };

                transaction.Wallet.Balance -= transaction.Amount;
                transaction.Wallet.UpdatedAt = DateTime.UtcNow;

                _context.Set<WalletTransaction>().Add(expiryTransaction);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed {Count} expired credit transactions", expiredTransactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired credits");
        }
    }

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await _context.Set<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
        
        if (wallet == null)
        {
            wallet = new Wallet { UserId = userId };
            _context.Set<Wallet>().Add(wallet);
            await _context.SaveChangesAsync();
        }

        return wallet;
    }
}
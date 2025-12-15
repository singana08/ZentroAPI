using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZentroAPI.Services;

public class ReferralBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReferralBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run daily

    public ReferralBackgroundService(IServiceProvider serviceProvider, ILogger<ReferralBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
                
                _logger.LogInformation("Processing expired referral credits...");
                await walletService.ProcessExpiredCreditsAsync();
                _logger.LogInformation("Completed processing expired referral credits");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired referral credits");
            }

            await Task.Delay(_period, stoppingToken);
        }
    }
}
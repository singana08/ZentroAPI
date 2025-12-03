using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.DTOs;

namespace ZentroAPI.Services;

public class ProviderMatchingService : IProviderMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProviderMatchingService> _logger;

    public ProviderMatchingService(ApplicationDbContext context, ILogger<ProviderMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProviderMatchDto>> FindMatchingProvidersAsync(Guid serviceRequestId)
    {
        var serviceRequest = await _context.ServiceRequests
            .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

        if (serviceRequest == null)
            return new List<ProviderMatchDto>();

        var providers = await _context.Providers
            .Include(p => p.User)
            .Where(p => p.IsActive)
            .ToListAsync();

        var matches = new List<ProviderMatchDto>();

        foreach (var provider in providers)
        {
            var matchScore = await CalculateMatchScoreAsync(provider.Id, serviceRequestId);
            var reasons = GetMatchReasons(provider, serviceRequest, matchScore);

            matches.Add(new ProviderMatchDto
            {
                ProviderId = provider.Id,
                FullName = provider.User.FullName,
                ProfileImage = provider.User.ProfileImage,
                Rating = provider.Rating,
                ExperienceYears = provider.ExperienceYears,
                Bio = provider.Bio,
                MatchScore = matchScore,
                PricingModel = provider.PricingModel,
                MatchReasons = reasons
            });
        }

        return matches
            .Where(m => m.MatchScore > 0.3m) // Minimum match threshold
            .OrderByDescending(m => m.MatchScore)
            .ThenByDescending(m => m.Rating)
            .Take(10)
            .ToList();
    }

    public async Task<List<ProviderMatchDto>> GetRecommendedProvidersAsync(
        string category, double? latitude = null, double? longitude = null, int maxResults = 10)
    {
        var providers = await _context.Providers
            .Include(p => p.User)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.ExperienceYears)
            .Take(maxResults)
            .ToListAsync();

        return providers.Select(p => new ProviderMatchDto
        {
            ProviderId = p.Id,
            FullName = p.User.FullName,
            ProfileImage = p.User.ProfileImage,
            Rating = p.Rating,
            ExperienceYears = p.ExperienceYears,
            Bio = p.Bio,
            MatchScore = CalculateRecommendationScore(p, category),
            PricingModel = p.PricingModel,
            MatchReasons = GetRecommendationReasons(p, category)
        }).ToList();
    }

    public async Task<decimal> CalculateMatchScoreAsync(Guid providerId, Guid serviceRequestId)
    {
        var provider = await _context.Providers
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == providerId);

        var serviceRequest = await _context.ServiceRequests
            .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

        if (provider == null || serviceRequest == null)
            return 0;

        decimal score = 0;

        // Rating weight (40%)
        score += (provider.Rating / 5) * 0.4m;

        // Experience weight (30%)
        var experienceScore = Math.Min(provider.ExperienceYears / 10m, 1m);
        score += experienceScore * 0.3m;

        // Category match weight (20%)
        // TODO: Implement actual category matching when provider categories are available
        score += 0.2m; // Placeholder

        // Availability weight (10%)
        // TODO: Check provider availability
        score += 0.1m; // Placeholder

        return Math.Min(score, 1m);
    }

    private List<string> GetMatchReasons(Models.Provider provider, Models.ServiceRequest serviceRequest, decimal matchScore)
    {
        var reasons = new List<string>();

        if (provider.Rating >= 4.5m)
            reasons.Add("Highly rated provider");

        if (provider.ExperienceYears >= 5)
            reasons.Add($"{provider.ExperienceYears} years of experience");

        if (matchScore >= 0.8m)
            reasons.Add("Excellent match for your requirements");

        return reasons;
    }

    private decimal CalculateRecommendationScore(Models.Provider provider, string category)
    {
        decimal score = 0;

        // Rating (50%)
        score += (provider.Rating / 5) * 0.5m;

        // Experience (30%)
        score += Math.Min(provider.ExperienceYears / 10m, 1m) * 0.3m;

        // Activity (20%)
        score += 0.2m; // Placeholder for provider activity score

        return score;
    }

    private List<string> GetRecommendationReasons(Models.Provider provider, string category)
    {
        var reasons = new List<string>();

        if (provider.Rating >= 4.5m)
            reasons.Add("Top-rated provider");

        if (provider.ExperienceYears >= 5)
            reasons.Add("Experienced professional");

        return reasons;
    }
}
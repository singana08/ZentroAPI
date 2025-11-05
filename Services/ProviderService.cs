using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HaluluAPI.Services;

/// <summary>
/// Service implementation for provider operations
/// </summary>
public class ProviderService : IProviderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(ApplicationDbContext context, ILogger<ProviderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProviderDto> RegisterProvider(Guid userId, CreateProviderDto dto)
    {
        try
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Check if provider already exists
            var existingProvider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProvider != null)
                throw new InvalidOperationException("User already has a provider profile");

            // Create new provider profile
            var provider = new Provider
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Bio = dto.Bio,
                ExperienceYears = dto.ExperienceYears,
                ServiceCategories = dto.ServiceCategories,
                ServiceAreas = dto.ServiceAreas,
                PricingModel = dto.PricingModel,
                Documents = dto.DocumentIds != null ? JsonSerializer.Serialize(dto.DocumentIds) : null,
                AvailabilitySlots = dto.AvailabilitySlots,
                Rating = 0,
                Earnings = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider profile created for user {userId}");

            return MapToDto(provider, user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating provider profile: {ex.Message}");
            throw;
        }
    }

    public async Task<ProviderDto?> GetProviderByUserId(Guid userId)
    {
        try
        {
            var provider = await _context.Providers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return null;

            return MapToDto(provider, provider.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider by user ID: {ex.Message}");
            throw;
        }
    }

    public async Task<ProviderDto?> GetProviderById(Guid providerId)
    {
        try
        {
            var provider = await _context.Providers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null)
                return null;

            return MapToDto(provider, provider.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider by ID: {ex.Message}");
            throw;
        }
    }

    public async Task<ProviderDto> UpdateProviderProfile(Guid userId, UpdateProviderDto dto)
    {
        try
        {
            var provider = await _context.Providers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                throw new InvalidOperationException("Provider profile not found");

            // Update fields
            if (!string.IsNullOrEmpty(dto.Bio))
                provider.Bio = dto.Bio;

            if (dto.ExperienceYears.HasValue)
                provider.ExperienceYears = dto.ExperienceYears.Value;

            if (!string.IsNullOrEmpty(dto.ServiceCategories))
                provider.ServiceCategories = dto.ServiceCategories;

            if (!string.IsNullOrEmpty(dto.ServiceAreas))
                provider.ServiceAreas = dto.ServiceAreas;

            if (!string.IsNullOrEmpty(dto.PricingModel))
                provider.PricingModel = dto.PricingModel;

            if (dto.DocumentIds != null)
                provider.Documents = JsonSerializer.Serialize(dto.DocumentIds);

            if (!string.IsNullOrEmpty(dto.AvailabilitySlots))
                provider.AvailabilitySlots = dto.AvailabilitySlots;

            if (dto.IsActive.HasValue)
                provider.IsActive = dto.IsActive.Value;

            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider profile updated for user {userId}");

            return MapToDto(provider, provider.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating provider profile: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsActiveProvider(Guid userId)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            return provider?.IsActive ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking if user is active provider: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeactivateProvider(Guid userId)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return false;

            provider.IsActive = false;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider profile deactivated for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deactivating provider: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ActivateProvider(Guid userId)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return false;

            provider.IsActive = true;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider profile activated for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error activating provider: {ex.Message}");
            throw;
        }
    }

    public async Task<ProviderProfileDto?> GetProviderProfileWithStats(Guid userId)
    {
        try
        {
            var provider = await _context.Providers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return null;

            // TODO: Get statistics from ServiceBid, Review, and ServiceRequest tables
            // For now, returning default values

            return new ProviderProfileDto
            {
                Id = provider.Id,
                UserId = provider.UserId,
                Bio = provider.Bio,
                ExperienceYears = provider.ExperienceYears,
                ServiceCategories = provider.ServiceCategories,
                ServiceAreas = provider.ServiceAreas,
                PricingModel = provider.PricingModel,
                DocumentIds = ParseJsonArray(provider.Documents),
                AvailabilitySlots = provider.AvailabilitySlots,
                Rating = provider.Rating,
                Earnings = provider.Earnings,
                IsActive = provider.IsActive,
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt,
                UserFullName = provider.User?.FullName,
                UserEmail = provider.User?.Email,
                UserPhoneNumber = provider.User?.PhoneNumber,
                UserProfileImage = provider.User?.ProfileImage,
                TotalJobsCompleted = 0, // TODO: Calculate from ServiceRequest where provider accepted
                ActiveJobs = 0, // TODO: Calculate from ServiceRequest with status Confirmed
                TotalReviews = 0, // TODO: Calculate from Review table
                AverageResponseTime = 0, // TODO: Calculate from bids
                BidAcceptanceRate = 0, // TODO: Calculate from bids
                IsVerified = false // TODO: Calculate from documents verification status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider profile with stats: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ProviderListDto>> SearchProviders(string? category, string? serviceArea, decimal? minRating = null)
    {
        try
        {
            var query = _context.Providers
                .Include(p => p.User)
                .Where(p => p.IsActive);

            // Filter by category if provided
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.ServiceCategories != null && p.ServiceCategories.Contains(category));
            }

            // Filter by service area if provided
            if (!string.IsNullOrEmpty(serviceArea))
            {
                query = query.Where(p => p.ServiceAreas != null && p.ServiceAreas.Contains(serviceArea));
            }

            // Filter by minimum rating if provided
            if (minRating.HasValue)
            {
                query = query.Where(p => p.Rating >= minRating.Value);
            }

            var providers = await query
                .OrderByDescending(p => p.Rating)
                .ToListAsync();

            return providers.Select(p => new ProviderListDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserFullName = p.User?.FullName,
                UserProfileImage = p.User?.ProfileImage,
                Bio = p.Bio,
                ExperienceYears = p.ExperienceYears,
                Rating = p.Rating,
                TotalReviews = 0, // TODO: Get from Review table
                ServiceCategories = string.IsNullOrEmpty(p.ServiceCategories) 
                    ? new List<string>() 
                    : p.ServiceCategories.Split(',').ToList(),
                PricingModel = p.PricingModel,
                IsAvailable = !string.IsNullOrEmpty(p.AvailabilitySlots)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching providers: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateProviderRating(Guid providerId, decimal newRating)
    {
        try
        {
            var provider = await _context.Providers.FindAsync(providerId);
            if (provider == null)
                return false;

            provider.Rating = newRating;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider {providerId} rating updated to {newRating}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating provider rating: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateProviderEarnings(Guid providerId, decimal amount)
    {
        try
        {
            var provider = await _context.Providers.FindAsync(providerId);
            if (provider == null)
                return false;

            provider.Earnings += amount;
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider {providerId} earnings updated by {amount}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating provider earnings: {ex.Message}");
            throw;
        }
    }

    public async Task<Dictionary<string, AvailabilitySlotDto>?> GetProviderAvailability(Guid providerId)
    {
        try
        {
            var provider = await _context.Providers.FindAsync(providerId);
            if (provider == null || string.IsNullOrEmpty(provider.AvailabilitySlots))
                return null;

            // Parse JSONB to dictionary
            var availability = JsonSerializer.Deserialize<Dictionary<string, AvailabilitySlotDto>>(
                provider.AvailabilitySlots);

            return availability;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting provider availability: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateProviderAvailability(Guid userId, List<AvailabilitySlotDto> slots)
    {
        try
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (provider == null)
                return false;

            // Convert slots to dictionary
            var availabilityDict = slots.ToDictionary(
                s => s.DayOfWeek,
                s => new AvailabilitySlotDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsAvailable = s.IsAvailable,
                    Notes = s.Notes
                });

            provider.AvailabilitySlots = JsonSerializer.Serialize(availabilityDict);
            provider.UpdatedAt = DateTime.UtcNow;

            _context.Providers.Update(provider);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Provider {userId} availability updated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating provider availability: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ProviderListDto>> GetAllActiveProviders(int page = 1, int pageSize = 20)
    {
        try
        {
            var providers = await _context.Providers
                .Include(p => p.User)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Rating)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return providers.Select(p => new ProviderListDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserFullName = p.User?.FullName,
                UserProfileImage = p.User?.ProfileImage,
                Bio = p.Bio,
                ExperienceYears = p.ExperienceYears,
                Rating = p.Rating,
                TotalReviews = 0,
                ServiceCategories = string.IsNullOrEmpty(p.ServiceCategories)
                    ? new List<string>()
                    : p.ServiceCategories.Split(',').ToList(),
                PricingModel = p.PricingModel,
                IsAvailable = !string.IsNullOrEmpty(p.AvailabilitySlots)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting all active providers: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Map Provider entity to ProviderDto
    /// </summary>
    private ProviderDto MapToDto(Provider provider, User user)
    {
        return new ProviderDto
        {
            Id = provider.Id,
            UserId = provider.UserId,
            Bio = provider.Bio,
            ExperienceYears = provider.ExperienceYears,
            ServiceCategories = provider.ServiceCategories,
            ServiceAreas = provider.ServiceAreas,
            PricingModel = provider.PricingModel,
            DocumentIds = ParseJsonArray(provider.Documents),
            AvailabilitySlots = provider.AvailabilitySlots,
            Rating = provider.Rating,
            Earnings = provider.Earnings,
            IsActive = provider.IsActive,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt,
            UserFullName = user.FullName,
            UserEmail = user.Email,
            UserPhoneNumber = user.PhoneNumber,
            UserProfileImage = user.ProfileImage
        };
    }

    /// <summary>
    /// Parse JSON array string to List<string>
    /// </summary>
    private List<string>? ParseJsonArray(string? jsonArray)
    {
        if (string.IsNullOrEmpty(jsonArray))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(jsonArray);
        }
        catch
        {
            return null;
        }
    }
}
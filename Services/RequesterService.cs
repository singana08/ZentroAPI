using HaluluAPI.Data;
using HaluluAPI.DTOs;
using HaluluAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HaluluAPI.Services;

/// <summary>
/// Service implementation for requester operations
/// </summary>
public class RequesterService : IRequesterService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RequesterService> _logger;

    public RequesterService(ApplicationDbContext context, ILogger<RequesterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RequesterDto> RegisterRequester(Guid userId, CreateRequesterDto dto)
    {
        try
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if requester already exists
            var existingRequester = await _context.Requesters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (existingRequester != null)
            {
                throw new InvalidOperationException("User already has a requester profile");
            }

            // Create new requester profile
            var requester = new Requester
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Requesters.Add(requester);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Requester profile created for user {userId}");

            return MapToDto(requester, user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating requester profile: {ex.Message}");
            throw;
        }
    }

    public async Task<RequesterDto?> GetRequesterByUserId(Guid userId)
    {
        try
        {
            var requester = await _context.Requesters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (requester == null)
                return null;

            return MapToDto(requester, requester.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting requester by user ID: {ex.Message}");
            throw;
        }
    }

    public async Task<RequesterDto?> GetRequesterById(Guid requesterId)
    {
        try
        {
            var requester = await _context.Requesters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requesterId);

            if (requester == null)
                return null;

            return MapToDto(requester, requester.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting requester by ID: {ex.Message}");
            throw;
        }
    }

    public async Task<RequesterDto> UpdateRequesterProfile(Guid userId, UpdateRequesterDto dto)
    {
        try
        {
            var requester = await _context.Requesters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (requester == null)
                throw new InvalidOperationException("Requester profile not found");

            // Update fields
            if (dto.IsActive.HasValue)
                requester.IsActive = dto.IsActive.Value;

            requester.UpdatedAt = DateTime.UtcNow;

            _context.Requesters.Update(requester);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Requester profile updated for user {userId}");

            return MapToDto(requester, requester.User!);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating requester profile: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsActiveRequester(Guid userId)
    {
        try
        {
            var requester = await _context.Requesters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            return requester?.IsActive ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking if user is active requester: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeactivateRequester(Guid userId)
    {
        try
        {
            var requester = await _context.Requesters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (requester == null)
                return false;

            requester.IsActive = false;
            requester.UpdatedAt = DateTime.UtcNow;

            _context.Requesters.Update(requester);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Requester profile deactivated for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deactivating requester: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ActivateRequester(Guid userId)
    {
        try
        {
            var requester = await _context.Requesters
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (requester == null)
                return false;

            requester.IsActive = true;
            requester.UpdatedAt = DateTime.UtcNow;

            _context.Requesters.Update(requester);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Requester profile activated for user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error activating requester: {ex.Message}");
            throw;
        }
    }

    public async Task<RequesterProfileDto?> GetRequesterProfileWithStats(Guid userId)
    {
        try
        {
            var requester = await _context.Requesters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (requester == null)
                return null;

            // Get statistics
            var serviceRequests = await _context.ServiceRequests
                .Where(sr => sr.RequesterId == requester.Id)
                .ToListAsync();

            var totalRequests = serviceRequests.Count;
            var activeRequests = serviceRequests.Count(sr => sr.Status == ServiceRequestStatus.Pending || sr.Status == ServiceRequestStatus.Confirmed);
            var completedRequests = serviceRequests.Count(sr => sr.Status == ServiceRequestStatus.Completed);

            // Calculate average provider rating from reviews (if you have a Review table)
            var averageRating = 0m; // TODO: Calculate from Review table

            return new RequesterProfileDto
            {
                Id = requester.Id,
                UserId = requester.UserId,

                IsActive = requester.IsActive,
                CreatedAt = requester.CreatedAt,
                UpdatedAt = requester.UpdatedAt,
                UserFullName = requester.User?.FullName,
                UserEmail = requester.User?.Email,
                UserPhoneNumber = requester.User?.PhoneNumber,
                UserProfileImage = requester.User?.ProfileImage,
                TotalServiceRequests = totalRequests,
                ActiveRequests = activeRequests,
                CompletedRequests = completedRequests,
                AverageProviderRating = averageRating
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting requester profile with stats: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Map Requester entity to RequesterDto
    /// </summary>
    private RequesterDto MapToDto(Requester requester, User user)
    {
        return new RequesterDto
        {
            Id = requester.Id,
            UserId = requester.UserId,

            IsActive = requester.IsActive,
            CreatedAt = requester.CreatedAt,
            UpdatedAt = requester.UpdatedAt,
            UserFullName = user.FullName,
            UserEmail = user.Email,
            UserPhoneNumber = user.PhoneNumber,
            UserProfileImage = user.ProfileImage
        };
    }
}
using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserController> _logger;

    public UserController(ApplicationDbContext context, ITokenService tokenService, ILogger<UserController> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Get user profile data based on profile ID from token
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
            }

            // Check if it's a requester profile
            var requester = await _context.Requesters
                .Include(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == profileId.Value);

            if (requester != null)
            {
                var userWithProfiles = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == requester.UserId);
                
                // Calculate requester statistics with logging
                var totalBookings = await _context.ServiceRequests
                    .AsNoTracking()
                    .CountAsync(sr => sr.RequesterId == requester.Id);
                    
                var completedBookings = await _context.ServiceRequests
                    .AsNoTracking()
                    .CountAsync(sr => sr.RequesterId == requester.Id && sr.Status == Models.ServiceRequestStatus.Completed);
                    
                // Debug: Get actual service requests for this requester
                var debugRequests = await _context.ServiceRequests
                    .AsNoTracking()
                    .Where(sr => sr.RequesterId == requester.Id)
                    .Select(sr => new { sr.Id, sr.Status, sr.CreatedAt })
                    .ToListAsync();
                    
                _logger.LogInformation($"Requester {requester.Id}: TotalBookings={totalBookings}, CompletedBookings={completedBookings}");
                _logger.LogInformation($"Debug - Service requests: {string.Join(", ", debugRequests.Select(r => $"{r.Id}:{r.Status}"))}");
                
                return Ok(new ProfileDto
                {
                    Id = requester.Id,
                    ProfileType = "REQUESTER",
                    FullName = requester.User?.FullName ?? "Unknown",
                    Email = requester.User?.Email ?? "Unknown",
                    PhoneNumber = requester.User?.PhoneNumber ?? "Unknown",
                    ProfileImage = requester.User?.ProfileImage,
                    Address = requester.User?.Address,
                    IsActive = requester.IsActive,
                    CreatedAt = requester.CreatedAt,
                    HasRequesterProfile = userWithProfiles?.RequesterProfile != null,
                    HasProviderProfile = userWithProfiles?.ProviderProfile != null,
                    IsProfileCompleted = userWithProfiles?.IsProfileCompleted ?? false,
                    DefaultRole = userWithProfiles?.DefaultRole,
                    TotalBookings = totalBookings,
                    CompletedBookings = completedBookings
                });
            }

            // Check if it's a provider profile
            var provider = await _context.Providers
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == profileId.Value);

            if (provider != null)
            {
                var userWithProfiles = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == provider.UserId);
                
                return Ok(new ProfileDto
                {
                    Id = provider.Id,
                    ProfileType = "PROVIDER",
                    FullName = provider.User?.FullName ?? string.Empty,
                    Email = provider.User?.Email ?? string.Empty,
                    PhoneNumber = provider.User?.PhoneNumber ?? string.Empty,
                    ProfileImage = provider.User?.ProfileImage,
                    Address = provider.User?.Address,
                    IsActive = provider.IsActive,
                    CreatedAt = provider.CreatedAt,
                    HasRequesterProfile = userWithProfiles?.RequesterProfile != null,
                    HasProviderProfile = userWithProfiles?.ProviderProfile != null,
                    IsProfileCompleted = userWithProfiles?.IsProfileCompleted ?? false,
                    DefaultRole = userWithProfiles?.DefaultRole,
                    ServiceCategories = string.IsNullOrEmpty(provider.ServiceCategories) ? null : 
                        JsonSerializer.Deserialize<string[]>(provider.ServiceCategories),
                    ExperienceYears = provider.ExperienceYears,
                    Bio = provider.Bio,
                    ServiceAreas = string.IsNullOrEmpty(provider.ServiceAreas) ? null : 
                        JsonSerializer.Deserialize<string[]>(provider.ServiceAreas),
                    PricingModel = provider.PricingModel,
                    Rating = provider.Rating,
                    Earnings = provider.Earnings
                });
            }

            return NotFound(new ErrorResponse { Message = "Profile not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProfile endpoint");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user profile (Requester or Provider based on current role)
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
            {
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });
            }

            // Check if it's a requester profile
            var requester = await _context.Requesters
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == profileId.Value);

            if (requester != null)
            {
                // Update User table fields
                bool userUpdated = false;
                if (!string.IsNullOrEmpty(request.FullName) && request.FullName != requester.User!.FullName)
                {
                    requester.User.FullName = request.FullName;
                    userUpdated = true;
                }

                if (!string.IsNullOrEmpty(request.Address) && request.Address != requester.User.Address)
                {
                    requester.User.Address = request.Address;
                    userUpdated = true;
                }

                if (!string.IsNullOrEmpty(request.ProfileImage) && request.ProfileImage != requester.User.ProfileImage)
                {
                    requester.User.ProfileImage = request.ProfileImage;
                    userUpdated = true;
                }

                if (userUpdated)
                {
                    requester.User.UpdatedAt = DateTime.UtcNow;
                }

                requester.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Requester profile updated: {profileId.Value}");
                return Ok(new { Success = true, Message = "Profile updated successfully" });
            }

            // Check if it's a provider profile
            var provider = await _context.Providers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == profileId.Value);

            if (provider != null)
            {
                // Update User table fields
                bool userUpdated = false;
                if (!string.IsNullOrEmpty(request.FullName) && request.FullName != provider.User!.FullName)
                {
                    provider.User.FullName = request.FullName;
                    userUpdated = true;
                }

                if (!string.IsNullOrEmpty(request.Address) && request.Address != provider.User.Address)
                {
                    provider.User.Address = request.Address;
                    userUpdated = true;
                }

                if (!string.IsNullOrEmpty(request.ProfileImage) && request.ProfileImage != provider.User.ProfileImage)
                {
                    provider.User.ProfileImage = request.ProfileImage;
                    userUpdated = true;
                }

                if (userUpdated)
                {
                    provider.User.UpdatedAt = DateTime.UtcNow;
                }

                // Update Provider-specific fields
                bool providerUpdated = false;

                if (!string.IsNullOrEmpty(request.Bio) && request.Bio != provider.Bio)
                {
                    provider.Bio = request.Bio;
                    providerUpdated = true;
                }

                if (request.ExperienceYears.HasValue && request.ExperienceYears.Value != provider.ExperienceYears)
                {
                    provider.ExperienceYears = request.ExperienceYears.Value;
                    providerUpdated = true;
                }

                if (!string.IsNullOrEmpty(request.PricingModel) && request.PricingModel != provider.PricingModel)
                {
                    provider.PricingModel = request.PricingModel;
                    providerUpdated = true;
                }

                if (request.ServiceAreas != null)
                {
                    var serviceAreasJson = JsonSerializer.Serialize(request.ServiceAreas);
                    if (serviceAreasJson != provider.ServiceAreas)
                    {
                        provider.ServiceAreas = serviceAreasJson;
                        providerUpdated = true;
                    }
                }

                if (request.ServiceCategories != null)
                {
                    var serviceCategoriesJson = JsonSerializer.Serialize(request.ServiceCategories);
                    if (serviceCategoriesJson != provider.ServiceCategories)
                    {
                        provider.ServiceCategories = serviceCategoriesJson;
                        providerUpdated = true;
                    }
                }

                if (providerUpdated)
                {
                    provider.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Provider profile updated: {profileId.Value}");
                return Ok(new { Success = true, Message = "Profile updated successfully" });
            }

            return NotFound(new ErrorResponse { Message = "Profile not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }
}

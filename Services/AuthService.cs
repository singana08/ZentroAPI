using ZentroAPI.Data;
using ZentroAPI.DTOs;
using ZentroAPI.Models;
using ZentroAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ZentroAPI.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IJwtService _jwtService;
    private readonly IReferralService _referralService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IOtpService otpService,
        IEmailService emailService,
        IJwtService jwtService,
        IReferralService referralService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _otpService = otpService;
        _emailService = emailService;
        _jwtService = jwtService;
        _referralService = referralService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, int ExpirationMinutes)> SendOtpAsync(
        string? email,
        string? phoneNumber)
    {
        try
        {
            // Validate that at least one identifier is provided
            if ((string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber)))
            {
                return (false, "Either email or phone number is required", 0);
            }

            // Use email as primary identifier if available
            string identifier = !string.IsNullOrWhiteSpace(email) ? email : phoneNumber!;
            string identifierType = !string.IsNullOrWhiteSpace(email) ? "email" : "phone";

            // Validate format
            if (identifierType == "email" && !identifier.Contains("@"))
            {
                return (false, "Invalid email address", 0);
            }

            // Check if identifier is locked
            if (await _otpService.IsEmailLockedAsync(identifier))
            {
                return (false, "This identifier is temporarily locked due to too many failed attempts. Please try again later.", 0);
            }

            // Generate OTP
            var (otpCode, expiresAt) = await _otpService.GenerateOtpAsync(identifier, null, OtpPurpose.Authentication);

            // Send OTP via appropriate channel
            bool otpSent;
            string successMessage;

            if (identifierType == "email")
            {
                otpSent = await _emailService.SendOtpEmailAsync(identifier, otpCode, 5);
                successMessage = "OTP sent successfully to your email";
            }
            else
            {
                // For phone, we would use SMS service if available
                // For now, we'll just log it (you can integrate Twilio or similar)
                _logger.LogInformation($"OTP for phone {identifier}: {otpCode}");
                otpSent = true;
                successMessage = "OTP sent successfully to your phone";
            }

            if (!otpSent)
            {
                _logger.LogWarning($"Failed to send OTP to {identifier}");
                return (false, $"Failed to send OTP. Please try again.", 0);
            }

            _logger.LogInformation($"OTP sent successfully to {identifierType}: {identifier}");
            return (true, successMessage, 5);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending OTP");
            return (false, "An error occurred while sending OTP. Please try again.", 0);
        }
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User, bool IsNewUser)> VerifyOtpAsync(
        string? email,
        string? phoneNumber,
        string otpCode)
    {
        try
        {
            if ((string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber)) || string.IsNullOrWhiteSpace(otpCode))
            {
                return (false, "Email/Phone and OTP are required", null, null, false);
            }

            // Use email as primary identifier if available
            string identifier = !string.IsNullOrWhiteSpace(email) ? email : phoneNumber!;
            bool verifiedViaEmail = !string.IsNullOrWhiteSpace(email);

            // Verify OTP
            var isValid = await _otpService.VerifyOtpAsync(identifier, otpCode, OtpPurpose.Authentication);

            if (!isValid)
            {
                return (false, "Invalid or expired OTP", null, null, false);
            }

            // Check if user exists by email or phone
            User? user = null;

            if (!string.IsNullOrWhiteSpace(email))
            {
                user = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user == null && !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber != "unknown")
            {
                user = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            }

            bool isNewUser = false;

            if (user == null)
            {
                // Create new user (auto-registration)
                isNewUser = true;
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email ?? phoneNumber ?? "unknown",
                    PhoneNumber = phoneNumber ?? "unknown",
                    FullName = "User", // Temporary name, will be updated during registration
                    CreatedAt = DateTime.UtcNow,
                    IsEmailVerified = verifiedViaEmail,
                    IsPhoneVerified = !verifiedViaEmail && !string.IsNullOrWhiteSpace(phoneNumber)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user created automatically: {identifier}");
            }
            else
            {
                // Mark identifier as verified
                if (verifiedViaEmail)
                {
                    user.IsEmailVerified = true;
                }
                else if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber != "unknown")
                {
                    user.IsPhoneVerified = true;
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            var userDto = MapToUserDto(user);

            // Only generate token if profile is completed
            string? token = null;
            if (user.IsProfileCompleted)
            {
                string activeRole = user.DefaultRole;
                // Get profile ID based on default role
                Guid profileId = Guid.Empty;
                if (activeRole == "PROVIDER" && user.ProviderProfile != null)
                {
                    profileId = user.ProviderProfile.Id;
                }
                else if (user.RequesterProfile != null)
                {
                    profileId = user.RequesterProfile.Id;
                }
                token = _jwtService.GenerateToken(user, activeRole, profileId);
                userDto = MapToUserDto(user, profileId);
            }

            _logger.LogInformation($"OTP verified successfully for user: {identifier}");
            return (true, "OTP verified successfully", token, userDto, isNewUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying OTP");
            return (false, "An error occurred while verifying OTP", null, null, false);
        }
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return (false, "Full name is required", null, null);
            }

            // Find user by email or phone (they should exist from OTP verification)
            User? existingUser = null;

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);
            }

            if (existingUser == null && !string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            }

            if (existingUser == null)
            {
                return (false, "User not found. Please verify OTP first.", null, null);
            }

            // Update core user identity data
            existingUser.FullName = request.FullName;
            if (!string.IsNullOrWhiteSpace(request.Email))
                existingUser.Email = request.Email;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                existingUser.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(request.Address))
                existingUser.Address = request.Address;
            existingUser.IsProfileCompleted = true;
            string roleText = request.Role == UserRoleDto.Provider ? "PROVIDER" : "REQUESTER";
            string defaultRoleText = roleText;
            existingUser.DefaultRole = defaultRoleText;
            existingUser.UpdatedAt = DateTime.UtcNow;
            
            // Generate referral code if not exists
            if (string.IsNullOrEmpty(existingUser.ReferralCode))
            {
                existingUser.ReferralCode = GenerateReferralCode();
            }

            // Handle role creation based on request
            string activeRole = "REQUESTER";
            Guid profileId = Guid.Empty;

            if (request.Role == UserRoleDto.Provider)
            {
                // Validate provider-specific fields
                if (request.ServiceCategories == null || request.ServiceCategories.Length == 0)
                {
                    return (false, "Service categories are required for providers", null, null);
                }

                // Create provider profile
                var provider = new Provider
                {
                    UserId = existingUser.Id,
                    ServiceCategories = JsonSerializer.Serialize(request.ServiceCategories),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Providers.Add(provider);
                profileId = provider.Id;

                activeRole = "PROVIDER";
            }
            else
            {
                // Create requester profile
                var requester = new Requester
                {
                    UserId = existingUser.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Requesters.Add(requester);
                profileId = requester.Id;

                activeRole = "REQUESTER";
            }

            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();

            // Generate JWT token with active role and profile ID
            var token = _jwtService.GenerateToken(existingUser, activeRole, profileId);

            // Handle referral code if provided
            if (!string.IsNullOrWhiteSpace(request.ReferralCode))
            {
                _logger.LogInformation($"Processing referral code {request.ReferralCode} for user {existingUser.Id}");
                var referralResult = await _referralService.UseReferralCodeAsync(existingUser.Id, request.ReferralCode);
                if (!referralResult.Success)
                {
                    _logger.LogError($"Referral code application failed for user {existingUser.Id}: {referralResult.Message}");
                    // Don't fail registration, just log the error
                }
                else
                {
                    _logger.LogInformation($"Referral code {request.ReferralCode} applied successfully for user {existingUser.Id}");
                }
            }

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(
                existingUser.Email,
                existingUser.FullName,
                activeRole);

            // Process referral completion (if user was referred)
            await _referralService.ProcessReferralCompletionAsync(existingUser.Id);

            _logger.LogInformation($"User profile completed: {existingUser.Email} as {activeRole}");
            return (true, "Profile registered successfully", token, MapToUserDto(existingUser, profileId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering user profile");
            return (false, "An error occurred while registering profile", null, null);
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync(string profileId)
    {
        try
        {
            if (!Guid.TryParse(profileId, out var guidId))
                return null;

            // First try to find user by profile ID (from NameIdentifier)
            var provider = await _context.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == guidId);
            var requester = await _context.Requesters.AsNoTracking().FirstOrDefaultAsync(r => r.Id == guidId);
            
            var userId = provider?.UserId ?? requester?.UserId;
            if (userId == null)
            {
                // Fallback: try as user ID directly
                var user = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == guidId);
                return user == null ? null : MapToUserDto(user);
            }

            // Get user by the found userId
            var foundUser = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return foundUser == null ? null : MapToUserDto(foundUser, guidId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current user: {profileId}");
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var user = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            return user == null ? null : MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by email: {email}");
            return null;
        }
    }

    public async Task<UserDto?> GetUserByPhoneAsync(string phoneNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber == "unknown")
                return null;

            var user = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            return user == null ? null : MapToUserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user by phone: {phoneNumber}");
            return null;
        }
    }

    private static UserDto MapToUserDto(User user, Guid? profileId = null)
    {
        return new UserDto
        {
            Id = profileId ?? user.RequesterProfile?.Id ?? user.ProviderProfile?.Id ?? user.Id,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            ProfileImage = user.ProfileImage,
            HasRequesterProfile = user.RequesterProfile != null,
            HasProviderProfile = user.ProviderProfile != null,
            IsProfileCompleted = user.IsProfileCompleted,
            DefaultRole = user.DefaultRole,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User)> AddProfileAsync(string userId, AddProfileRequest request)
    {
        try
        {
            if (!Guid.TryParse(userId, out var guidId))
                return (false, "Invalid user ID", null, null);

            var user = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .FirstOrDefaultAsync(u => u.Id == guidId);

            if (user == null)
                return (false, "User not found", null, null);

            if (request.Role == UserRoleDto.Provider)
            {
                if (user.ProviderProfile != null)
                    return (false, "Provider profile already exists", null, null);

                // Validate provider fields
                if (request.ServiceCategories == null || request.ServiceCategories.Length == 0)
                    return (false, "Service categories are required for providers", null, null);

                // Create provider profile
                var newProvider = new Provider
                {
                    UserId = user.Id,
                    ServiceCategories = JsonSerializer.Serialize(request.ServiceCategories),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Providers.Add(newProvider);
            }
            else
            {
                if (user.RequesterProfile != null)
                    return (false, "Requester profile already exists", null, null);

                // Create requester profile
                var newRequester = new Requester
                {
                    UserId = user.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Requesters.Add(newRequester);
            }

            // Update default role if requested
            if (request.SetAsDefault)
            {
                user.DefaultRole = request.Role == UserRoleDto.Provider ? "PROVIDER" : "REQUESTER";
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            // Reload user with new profile
            user = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            // Get the new profile ID
            Guid newProfileId = Guid.Empty;
            if (request.Role == UserRoleDto.Provider && user!.ProviderProfile != null)
            {
                newProfileId = user.ProviderProfile.Id;
            }
            else if (request.Role == UserRoleDto.Requester && user!.RequesterProfile != null)
            {
                newProfileId = user.RequesterProfile.Id;
            }

            // Generate new token if this profile is set as default
            string? newToken = null;
            if (request.SetAsDefault && newProfileId != Guid.Empty)
            {
                newToken = _jwtService.GenerateToken(user!, user.DefaultRole, newProfileId);
            }

            var userDto = MapToUserDto(user!, newProfileId != Guid.Empty ? newProfileId : null);
            return (true, "Profile added successfully", newToken, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adding profile for user ID: {userId}");
            return (false, "An error occurred while adding profile", null, null);
        }
    }

    public async Task<(bool Success, string Message, string? Token, UserDto? User)> SwitchRoleAsync(string userId, string newRole)
    {
        try
        {
            if (!Guid.TryParse(userId, out var guidId))
                return (false, "Invalid user ID", null, null);

            var user = await _context.Users
                .Include(u => u.RequesterProfile)
                .Include(u => u.ProviderProfile)
                .FirstOrDefaultAsync(u => u.Id == guidId);

            if (user == null)
                return (false, "User not found", null, null);

            // Validate role and check if profile exists
            Guid profileId;
            if (newRole == "PROVIDER")
            {
                if (user.ProviderProfile == null)
                    return (false, "Provider profile not found", null, null);
                profileId = user.ProviderProfile.Id;
            }
            else if (newRole == "REQUESTER")
            {
                if (user.RequesterProfile == null)
                    return (false, "Requester profile not found", null, null);
                profileId = user.RequesterProfile.Id;
            }
            else
            {
                return (false, "Invalid role. Must be PROVIDER or REQUESTER", null, null);
            }

            // Update default role
            user.DefaultRole = newRole;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Generate new token with new role and profile ID
            var token = _jwtService.GenerateToken(user, newRole, profileId);
            var userDto = MapToUserDto(user, profileId);

            _logger.LogInformation($"User {user.Email} switched to role: {newRole}");
            return (true, "Role switched successfully", token, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error switching role for user ID: {userId}");
            return (false, "An error occurred while switching role", null, null);
        }
    }

    public async Task<ProfileDto?> GetUserProfileAsync(string profileId)
    {
        try
        {
            _logger.LogInformation($"Looking up profile for ID: {profileId}");
            
            if (!Guid.TryParse(profileId, out var guidId))
            {
                _logger.LogWarning($"Invalid GUID format: {profileId}");
                return null;
            }

            // Check if it's a requester profile
            _logger.LogInformation($"Checking Requesters table for ID: {guidId}");
            var requester = await _context.Requesters
                .Include(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == guidId);

            if (requester != null)
            {
                _logger.LogInformation($"Found requester profile for ID: {guidId}, UserId: {requester.UserId}");
                _logger.LogInformation($"User loaded: {requester.User != null}, User details: {requester.User?.FullName}");
                
                // Load user with all profiles to get complete profile status
                var userWithProfiles = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == requester.UserId);
                
                return new ProfileDto
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
                    DefaultRole = userWithProfiles?.DefaultRole
                };
            }
            else
            {
                _logger.LogWarning($"No requester found for ID: {guidId}");
            }

            // Check if it's a provider profile
            _logger.LogInformation($"Checking Providers table for ID: {guidId}");
            var provider = await _context.Providers
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == guidId);

            if (provider != null)
            {
                _logger.LogInformation($"Found provider profile for ID: {guidId}");
                
                // Load user with all profiles to get complete profile status
                var userWithProfiles = await _context.Users
                    .Include(u => u.RequesterProfile)
                    .Include(u => u.ProviderProfile)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == provider.UserId);
                
                return new ProfileDto
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
                        System.Text.Json.JsonSerializer.Deserialize<string[]>(provider.ServiceCategories),
                    ExperienceYears = provider.ExperienceYears,
                    Bio = provider.Bio,
                    ServiceAreas = string.IsNullOrEmpty(provider.ServiceAreas) ? null : 
                        System.Text.Json.JsonSerializer.Deserialize<string[]>(provider.ServiceAreas),
                    PricingModel = provider.PricingModel,
                    Rating = provider.Rating,
                    Earnings = provider.Earnings
                };
            }

            _logger.LogWarning($"Profile not found in either Requesters or Providers table for ID: {guidId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user profile: {profileId}");
            return null;
        }
    }
    
    public async Task<(bool Success, string Message, TokenResponse? TokenResponse)> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenRecord = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u!.RequesterProfile)
                .Include(rt => rt.User)
                .ThenInclude(u => u!.ProviderProfile)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.IsActive);

            if (tokenRecord == null)
            {
                return (false, "Invalid or expired refresh token", null);
            }

            var user = tokenRecord.User!;
            
            // Determine active role and profile ID based on user's default role
            string activeRole = user.DefaultRole ?? "REQUESTER";
            Guid? profileId = null;
            
            if (activeRole == "PROVIDER" && user.ProviderProfile != null)
            {
                profileId = user.ProviderProfile.Id;
            }
            else if (user.RequesterProfile != null)
            {
                profileId = user.RequesterProfile.Id;
                activeRole = "REQUESTER";
            }

            // Generate new token response
            var tokenResponse = await _jwtService.GenerateTokenResponse(user, activeRole, profileId, tokenRecord.DeviceId);

            // Revoke old refresh token
            tokenRecord.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Token refreshed successfully", tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return (false, "Error refreshing token", null);
        }
    }

    public async Task<(bool Success, string Message)> LogoutAsync(string refreshToken)
    {
        try
        {
            var tokenRecord = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenRecord != null)
            {
                tokenRecord.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return (true, "Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return (false, "Error during logout");
        }
    }

    private string GenerateReferralCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

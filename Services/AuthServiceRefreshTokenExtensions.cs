using Microsoft.EntityFrameworkCore;
using ZentroAPI.Data;
using ZentroAPI.Models;

namespace ZentroAPI.Services;

public static class AuthServiceRefreshTokenExtensions
{
    public static async Task<(bool Success, string Message, TokenResponse? TokenResponse)> RefreshTokenAsync(
        this AuthService authService, 
        ApplicationDbContext context,
        IJwtService jwtService,
        string refreshToken)
    {
        try
        {
            var tokenRecord = await context.RefreshTokens
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
            
            // Determine active role and profile ID
            string activeRole = "REQUESTER";
            Guid? profileId = user.RequesterProfile?.Id;
            
            if (user.ProviderProfile != null)
            {
                activeRole = "PROVIDER";
                profileId = user.ProviderProfile.Id;
            }

            // Generate new token response
            var tokenResponse = await jwtService.GenerateTokenResponse(user, activeRole, profileId, tokenRecord.DeviceId);

            // Revoke old refresh token
            tokenRecord.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return (true, "Token refreshed successfully", tokenResponse);
        }
        catch (Exception ex)
        {
            return (false, "Error refreshing token", null);
        }
    }

    public static async Task<(bool Success, string Message)> LogoutAsync(
        this AuthService authService,
        ApplicationDbContext context,
        string refreshToken)
    {
        try
        {
            var tokenRecord = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenRecord != null)
            {
                tokenRecord.RevokedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            return (true, "Logged out successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error during logout");
        }
    }
}
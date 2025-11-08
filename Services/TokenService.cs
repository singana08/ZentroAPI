using System.Security.Claims;

namespace HaluluAPI.Services;

public class TokenService : ITokenService
{
    public (Guid? ProfileId, Guid? UserId, string? Role) ExtractTokenInfo(ClaimsPrincipal user)
    {
        var profileIdClaim = user.FindFirst("profile_id")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userIdClaim = user.FindFirst("user_id")?.Value;
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

        Guid? profileId = null;
        Guid? userId = null;

        if (!string.IsNullOrEmpty(profileIdClaim) && Guid.TryParse(profileIdClaim, out var parsedProfileId))
            profileId = parsedProfileId;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
            userId = parsedUserId;

        return (profileId, userId, roleClaim);
    }
}
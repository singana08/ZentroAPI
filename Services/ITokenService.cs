using System.Security.Claims;

namespace HaluluAPI.Services;

public interface ITokenService
{
    (Guid? ProfileId, Guid? UserId, string? Role) ExtractTokenInfo(ClaimsPrincipal user);
}
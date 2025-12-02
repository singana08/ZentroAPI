using System.Security.Claims;

namespace ZentroAPI.Services;

public interface ITokenService
{
    (Guid? ProfileId, Guid? UserId, string? Role) ExtractTokenInfo(ClaimsPrincipal user);
}

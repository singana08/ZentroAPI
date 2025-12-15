using Microsoft.AspNetCore.Mvc;
using ZentroAPI.Attributes;
using ZentroAPI.DTOs;
using ZentroAPI.Services;
using ZentroAPI.Utilities;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeRole("REQUESTER", "PROVIDER")]
public class ReferralController : ControllerBase
{
    private readonly IReferralService _referralService;
    private readonly IWalletService _walletService;

    public ReferralController(IReferralService referralService, IWalletService walletService)
    {
        _referralService = referralService;
        _walletService = walletService;
    }

    /// <summary>
    /// Get detailed referral statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetReferralStats()
    {
        var userIdString = User.GetUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized("User ID not found in token");

        var result = await _referralService.GetReferralStatsAsync(userId);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get user's wallet information
    /// </summary>
    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var userIdString = User.GetUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized("User ID not found in token");

        var result = await _walletService.GetWalletAsync(userId);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }
}
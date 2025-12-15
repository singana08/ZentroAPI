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
    /// Get user's referral code and basic stats
    /// </summary>
    [HttpGet("code")]
    public async Task<IActionResult> GetReferralCode()
    {
        var userIdString = User.GetUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized("User ID not found in token");

        var result = await _referralService.GetUserReferralCodeAsync(userId);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Apply a referral code (only for new users)
    /// </summary>
    [HttpPost("use")]
    public async Task<IActionResult> UseReferralCode([FromBody] UseReferralCodeRequest request)
    {
        var userIdString = User.GetUserId();
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized("User ID not found in token");

        var result = await _referralService.UseReferralCodeAsync(userId, request.ReferralCode);
        
        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(new { message = result.Message });
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
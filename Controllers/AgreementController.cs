using ZentroAPI.DTOs;
using ZentroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ZentroAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgreementController : ControllerBase
{
    private readonly IAgreementService _agreementService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AgreementController> _logger;

    public AgreementController(IAgreementService agreementService, ITokenService tokenService, ILogger<AgreementController> logger)
    {
        _agreementService = agreementService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Respond to agreement (accept or reject)
    /// </summary>
    [HttpPost("respond")]
    [Authorize]
    [ProducesResponseType(typeof(AgreementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RespondToAgreement([FromBody] AcceptAgreementDto request)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _agreementService.RespondToAgreementAsync(profileId.Value, request);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get agreement for a request
    /// </summary>
    [HttpGet("{requestId}")]
    [Authorize]
    [ProducesResponseType(typeof(AgreementResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAgreement([FromRoute] Guid requestId)
    {
        try
        {
            var (profileId, _, _) = _tokenService.ExtractTokenInfo(User);
            if (!profileId.HasValue)
                return Unauthorized(new ErrorResponse { Message = "Profile ID not found in token" });

            var (success, message, data) = await _agreementService.GetAgreementAsync(requestId, profileId.Value);
            if (!success)
                return BadRequest(new ErrorResponse { Message = message });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agreement");
            return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
        }
    }

}
